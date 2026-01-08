function OnLoad()
    CreateUI('{pluginName}', 300, 400)
    AddLabel('APA RENCANA KAMU HARI INI?')
    AddInput('task_input', '')
    AddButton('TAMBAH TUGAS', 'AddTask')
    
    -- Muat data lama dari file JSON
    local savedTasks = LoadConfig('tasks_data')
    if savedTasks ~= '' and savedTasks ~= nil then
        AddLabel('--- TUGAS TERSIMPAN ---')
        AddLabel(savedTasks)
    end
end

function AddTask()
    local task = GetInput('task_input')
    if task ~= '' and task ~= nil then
        AddLabel('• ' .. task)
        
        -- Simpan data (Append ke data lama atau simpan baru)
        local current = LoadConfig('tasks_data')
        local updated = current .. '\n• ' .. task
        SaveConfig('tasks_data', updated)
        
        Notify('Sukses', 'Tugas disimpan ke JSON!')
    else
        Notify('Peringatan', 'Isi tugasnya dulu Kak!')
    end
end