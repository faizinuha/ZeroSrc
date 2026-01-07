-- ZeroMix Plugin: Simple GUI Example
-- Deskripsi: Membuat GUI langsung dari Lua!

function OnLoad()
    -- Gak perlu ribet pake ZeroMix. lagi! Langsung aja:
    CreateUI("My Lua Widget", 300, 350)
    
    AddLabel("NAMA PLUGIN KAMU:")
    AddInput("nama_input", "Contoh Plugin")
    
    AddLabel("PESAN NOTIFIKASI:")
    AddInput("pesan_input", "Halo dari Lua GUI!")
    
    AddButton("KIRIM NOTIFIKASI", "ProsesKlik")
    
    Log("GUI Berhasil dimuat!")
end

function ProsesKlik()
    local nama = GetInput("nama_input")
    local pesan = GetInput("pesan_input")
    
    Notify(nama, pesan)
end
