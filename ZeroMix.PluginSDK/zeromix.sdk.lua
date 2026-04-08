--[[
    ZeroMix Plugin SDK - Lua Edition
    Version: 2.0.0
    
    Cara pakai di ZeroMix:
        local ZMX = require("zeromix.sdk")
    
    Cara pakai di OBS / Neovim / aplikasi Lua lain:
        local ZMX = dofile("/path/to/zeromix.sdk.lua")
    
    Download:
        curl -o zeromix.sdk.lua https://raw.githubusercontent.com/faizinuha/ZeroMix/main/ZeroMix.PluginSDK/zeromix.sdk.lua
--]]

local ZMX = {}

-- Deteksi environment
local _inZeroMix = type(SaveConfig) == "function"
local _hasSysAPI = type(GetCpuUsage) == "function"

-- ─── UI (khusus ZeroMix) ─────────────────────────────────────────────────────

ZMX.ui = {}

function ZMX.ui.create(title, width, height)
    if _inZeroMix then CreateUI(title, width or 400, height or 300) end
end

function ZMX.ui.label(text)
    if _inZeroMix then AddLabel(tostring(text)) end
end

function ZMX.ui.input(id, placeholder)
    if _inZeroMix then AddInput(id, placeholder or "") end
end

function ZMX.ui.button(text, callback)
    if _inZeroMix then AddButton(text, callback) end
end

function ZMX.ui.get(id)
    if _inZeroMix then return GetInput(id) or "" end
    return ""
end

function ZMX.ui.divider()
    if _inZeroMix then AddLabel("────────────────────────") end
end

-- ─── Notifikasi ───────────────────────────────────────────────────────────────

ZMX.notify = {}

function ZMX.notify.popup(title, message)
    if _inZeroMix then Notify(title, tostring(message)) end
end

function ZMX.notify.status(text)
    if _inZeroMix then SetStatusText(tostring(text)) end
end

function ZMX.notify.log(message)
    if _inZeroMix then
        Log("[ZMX] " .. tostring(message))
    else
        print("[ZMX] " .. tostring(message))
    end
end

-- ─── Sistem ───────────────────────────────────────────────────────────────────

ZMX.sys = {}

function ZMX.sys.cpu()
    return _hasSysAPI and (GetCpuUsage() or 0) or 0
end

function ZMX.sys.ram()
    return _hasSysAPI and (GetRamUsage() or 0) or 0
end

function ZMX.sys.hour()
    if _hasSysAPI then return GetTimeHour() or 0 end
    return tonumber(os.date("%H")) or 0
end

function ZMX.sys.minute()
    if _hasSysAPI then return GetTimeMin() or 0 end
    return tonumber(os.date("%M")) or 0
end

function ZMX.sys.time()
    return string.format("%02d:%02d", ZMX.sys.hour(), ZMX.sys.minute())
end

-- ─── Storage (semua platform) ─────────────────────────────────────────────────

ZMX.store = {}

function ZMX.store.set(key, value)
    if _inZeroMix then
        SaveConfig(key, tostring(value))
    else
        local f = io.open(key .. ".zmx", "w")
        if f then f:write(tostring(value)); f:close() end
    end
end

function ZMX.store.get(key, default)
    if _inZeroMix then
        local val = LoadConfig(key)
        if val == nil or val == "" then return default or "" end
        return val
    else
        local f = io.open(key .. ".zmx", "r")
        if f then
            local val = f:read("*a"); f:close()
            if val and val ~= "" then return val end
        end
        return default or ""
    end
end

function ZMX.store.set_table(key, tbl)
    if _inZeroMix then
        SaveConfig(key, JsonEncode(tbl))
    else
        local parts = {}
        for k, v in pairs(tbl) do
            parts[#parts+1] = tostring(k) .. "=" .. tostring(v)
        end
        ZMX.store.set(key, table.concat(parts, "\n"))
    end
end

function ZMX.store.get_table(key, default)
    if _inZeroMix then
        local json = LoadConfig(key)
        if json == nil or json == "" then return default or {} end
        local ok, result = pcall(JsonDecode, json)
        return ok and result or (default or {})
    else
        local raw = ZMX.store.get(key, "")
        if raw == "" then return default or {} end
        local tbl = {}
        for line in raw:gmatch("[^\n]+") do
            local k, v = line:match("^(.-)=(.*)$")
            if k then tbl[k] = v end
        end
        return tbl
    end
end

-- ─── Utils (semua platform) ───────────────────────────────────────────────────

ZMX.utils = {}

function ZMX.utils.empty(s)
    return s == nil or s == ""
end

function ZMX.utils.percent(n)
    return string.format("%.1f%%", n or 0)
end

function ZMX.utils.clamp(val, min, max)
    if val < min then return min end
    if val > max then return max end
    return val
end

-- ─── Info ─────────────────────────────────────────────────────────────────────

ZMX._version = "2.0.0"
ZMX._author  = "ZeroMix Team"

ZMX.notify.log("ZeroMix Lua SDK v" .. ZMX._version .. " loaded.")

return ZMX
