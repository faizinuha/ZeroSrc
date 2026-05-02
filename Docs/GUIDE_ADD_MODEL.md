# 📖 Cara Tambah Model Live2D ke Virtual Assistant

## Struktur Folder

```
bin/Debug/net9.0-windows/win-x64/Virtual_Assisten/
├── VA_Thumbnails/
│   ├── NamaKarakter.png   ← thumbnail card (300x400px recommended)
├── NamaFolder/
│   └── NamaModel/
│       ├── NamaModel.model3.json   ← file utama
│       ├── NamaModel.moc3
│       ├── NamaModel.physics3.json
│       ├── textures/
│       │   └── texture_00.png
│       ├── motions/
│       └── expressions/
```

## Langkah-langkah

### 1. Taruh file model
Copy folder model ke:
```
src/Virtual_Assisten/NamaFolder/NamaModel/
```

### 2. Tambah thumbnail
Taruh gambar thumbnail (PNG/JPG) di:
```
src/Virtual_Assisten/VA_Thumbnails/NamaKarakter.png
```

### 3. Daftarkan di `VirtualAssistantWindow.xaml.cs`

Buka `src/Virtual_Assisten/VirtualAssistantWindow.xaml.cs`, cari `GetModelPath()`:

```csharp
private string GetModelPath(string characterName)
{
    string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Virtual_Assisten");
    return characterName switch
    {
        "Fern"        => Path.Combine(baseDir, "Sou Sou No Frieren", "fern", "fern.model3.json"),
        "Huohuo"      => Path.Combine(baseDir, "Mihoyo", "Honkai_Star_Rail", "huohuo", "huohuo.model3.json"),
        "NamaKarakter" => Path.Combine(baseDir, "NamaFolder", "NamaModel", "NamaModel.model3.json"), // ← tambah ini
        _             => Path.Combine(baseDir, "Sou Sou No Frieren", "Frieren", "Frieren.model3.json")
    };
}
```

### 4. Tambah card di `MainWindow.xaml`

Cari section `<!-- VA Character Cards -->` di `src/MainWindow.xaml`, tambah card baru:

```xml
<!-- NamaKarakter Card -->
<Border Style="{StaticResource VACardBorder}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="150"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" CornerRadius="14,14,0,0" Background="#141922" ClipToBounds="True">
            <Image x:Name="NamaKarakterThumb" Stretch="UniformToFill" VerticalAlignment="Top"/>
        </Border>
        <StackPanel Grid.Row="1" Margin="16,12,16,8">
            <TextBlock Text="NamaKarakter" FontSize="16" FontWeight="Bold" Foreground="White"/>
            <TextBlock Text="Kelas / Ras" FontSize="12" Foreground="{StaticResource SubTextBrush}"/>
        </StackPanel>
        <Button Grid.Row="2" Content="ACTIVATE" Margin="16,0,16,16"
                Click="ActivateCharacter_Click" Tag="NamaKarakter"
                Style="{StaticResource VAActivateButton}"/>
    </Grid>
</Border>
```

### 5. Load thumbnail di `MainWindow.xaml.cs`

Cari `LoadVAThumbnails()`, tambah:

```csharp
(Image: NamaKarakterThumb, File: Path.Combine("Virtual_Assisten", "VA_Thumbnails", "NamaKarakter.png")),
```

### 6. Tambah expressions/motions di `live2d-viewer.html`

Cari `playIdleMotion()` di `src/Virtual_Assisten/live2d-viewer.html`:

```javascript
if (name.includes("namakarakter")) {
    expressions = ["exp1", "exp2", "exp3"]; // sesuai nama di model3.json
}
```

Dan `playBodyMotion()`:

```javascript
if (name.includes("namakarakter")) {
    const idx = Math.floor(Math.random() * 3); // jumlah motions
    model.motion("", idx);
}
```

### 7. Build

```bash
dotnet build ZeroMix.csproj -c Debug
```

## Tips

- Thumbnail optimal: **300×400px**, format PNG/JPG
- Model folder name harus **lowercase** untuk konsistensi path
- Cek nama expression di file `.model3.json` → bagian `"Expressions"`
- Cek nama motion di file `.model3.json` → bagian `"Motions"`
- Jika model berat (>20MB texture), tambah ke `isHeavyModel` check di `live2d-viewer.html`
