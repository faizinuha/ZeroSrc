-- ZeroMix Plugin: To-Do List
-- Deskripsi: Catat dan simpan tugas harian kamu

function OnLoad()
    CreateUI('To-Do List', 300, 420)
    AddLabel('APA RENCANA KAMU HARI INI?')
    AddInput('task_input', 'Tulis tugas di sini...')
    AddButton('TAMBAH TUGAS', 'AddTask')

    -- Muat tugas yang sudah tersimpan
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

        local current = LoadConfig('tasks_data')
        local updated = (current ~= '' and current ~= nil) and (current .. '\n• ' .. task) or ('• ' .. task)
        SaveConfig('tasks_data', updated)

        Notify('Sukses', 'Tugas berhasil disimpan!')
    else
        Notify('Peringatan', 'Isi tugasnya dulu Kak!')
    end
end
