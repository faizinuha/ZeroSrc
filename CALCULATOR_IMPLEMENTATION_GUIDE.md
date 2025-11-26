# 🧮 CALCULATOR FEATURE - IMPLEMENTATION GUIDE

## ✅ **YANG SUDAH DIBUAT:**

### **1. MathEvaluator.cs** - Pure C# Math Engine
**Location:** `c:\ZeroMix\ZeroMix\MathEvaluator.cs`

**Features:**
- ✅ Basic operations: `+`, `-`, `*`, `/`, `^`, `%`
- ✅ Functions: `sqrt()`, `abs()`, `round()`, `floor()`, `ceil()`, `sin()`, `cos()`, `tan()`, `log()`
- ✅ Constants: `pi`, `e`
- ✅ Parentheses support: `(2+3)*4`
- ✅ Smart detection: Auto-detect math expressions
- ✅ NO external dependencies - Pure C#

**Size:** ~5 KB

---

## 📝 **CARA INTEGRASI KE SearchOverlay.xaml.cs:**

### **Step 1: Update SuggestionType Enum**

**File:** `SearchOverlay.xaml.cs` (Line ~17-21)

**SEBELUM:**
```csharp
public enum SuggestionType
{
    App,
    WebSearch
}
```

**SESUDAH:**
```csharp
public enum SuggestionType
{
    App,
    WebSearch,
    Calculator  // ← ADD THIS
}
```

---

### **Step 2: Update SuggestionItem Constructor**

**File:** `SearchOverlay.xaml.cs` (Line ~30-38)

**SEBELUM:**
```csharp
public SuggestionItem(string displayText, string? filePath, SuggestionType type)
{
    DisplayText = displayText;
    FilePath = filePath ?? "";
    Type = type;
    // Tetapkan ikon berdasarkan tipe
    Icon = Type == SuggestionType.App ? "\uE770" : "\uE773"; // E770: App, E773: Web
}
```

**SESUDAH:**
```csharp
public SuggestionItem(string displayText, string? filePath, SuggestionType type)
{
    DisplayText = displayText;
    FilePath = filePath ?? "";
    Type = type;
    
    // Tetapkan ikon berdasarkan tipe
    Icon = Type switch
    {
        SuggestionType.App => "\uE770",        // App icon
        SuggestionType.Calculator => "\uE8EF", // Calculator icon
        _ => "\uE773"                           // Web search icon
    };
}
```

---

### **Step 3: Update SearchBox_TextChanged Method**

**File:** `SearchOverlay.xaml.cs` (Line ~197-243)

**TAMBAHKAN DI AWAL METHOD (setelah line 210):**

```csharp
private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
{
    if (_isSelectingSuggestion) return;

    var searchBox = sender as System.Windows.Controls.TextBox;
    string query = searchBox?.Text ?? "";
    var suggestionList = this.FindName("SuggestionList") as System.Windows.Controls.ListBox;

    if (string.IsNullOrWhiteSpace(query))
    {
        suggestionList!.ItemsSource = null;
        suggestionList.Visibility = Visibility.Collapsed;
        return;
    }

    // ========================================
    // 🧮 ADD CALCULATOR LOGIC HERE
    // ========================================
    var combined = new List<SuggestionItem>();

    // Check if query is a math expression
    if (MathEvaluator.IsMathExpression(query))
    {
        var (success, result, error) = MathEvaluator.Evaluate(query);
        
        if (success)
        {
            string formattedResult = MathEvaluator.FormatResult(result);
            
            // Add calculator result at TOP
            combined.Add(new SuggestionItem(
                displayText: $"= {formattedResult}",
                filePath: formattedResult, // Store for copy
                type: SuggestionType.Calculator
            ));
        }
    }

    // ========================================
    // 📱 IMPROVED FUZZY MATCHING
    // ========================================
    var localSuggestions = _allSuggestions
        .Select(s => new
        {
            Item = s,
            Score = CalculateMatchScore(s.DisplayText, query)
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .Select(x => x.Item)
        .Take(5)
        .ToList();

    combined.AddRange(localSuggestions);

    // ... rest of existing code (Google suggestions, etc.)
    var googleSuggestions = await GetGoogleSuggestionsAsync(query);
    var webSuggestions = googleSuggestions
        .Select(s => new SuggestionItem(s, null, SuggestionType.WebSearch))
        .ToList();

    combined.AddRange(webSuggestions);
    combined.Add(new SuggestionItem("Search Google for \"" + query + "\"", query, SuggestionType.WebSearch));

    if (combined.Count > 0)
    {
        var collectionView = new ListCollectionView(combined);
        collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
        suggestionList!.ItemsSource = collectionView;
        suggestionList.Visibility = Visibility.Visible;
    }
    else
    {
        suggestionList!.ItemsSource = null;
        suggestionList.Visibility = Visibility.Collapsed;
    }
}
```

---

### **Step 4: Add Fuzzy Match Score Method**

**File:** `SearchOverlay.xaml.cs` (ADD SETELAH `GetGoogleSuggestionsAsync`)

```csharp
/// <summary>
/// Calculate fuzzy match score (0-100)
/// Higher score = better match
/// </summary>
private int CalculateMatchScore(string text, string query)
{
    if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
        return 0;

    text = text.ToLower();
    query = query.ToLower();

    // Exact match = 100
    if (text == query) return 100;

    // Starts with = 90
    if (text.StartsWith(query)) return 90;

    // Contains at word boundary = 70
    if (text.Contains(" " + query)) return 70;

    // Contains anywhere = 50
    if (text.Contains(query)) return 50;

    // Fuzzy match (all characters present in order) = 30
    int queryIndex = 0;
    for (int i = 0; i < text.Length && queryIndex < query.Length; i++)
    {
        if (text[i] == query[queryIndex])
            queryIndex++;
    }
    if (queryIndex == query.Length) return 30;

    // No match
    return 0;
}
```

---

### **Step 5: Update HandleSuggestionSelection Method**

**File:** `SearchOverlay.xaml.cs` (Line ~321-341)

**TAMBAHKAN CASE UNTUK CALCULATOR:**

```csharp
private void HandleSuggestionSelection(SuggestionItem? selectedItem)
{
    if (selectedItem == null) return;

    if (selectedItem.Type == SuggestionType.App)
    {
        // Langsung jalankan aplikasi
        ExecuteCommand(selectedItem.DisplayText);
    }
    // ========================================
    // 🧮 ADD CALCULATOR HANDLER
    // ========================================
    else if (selectedItem.Type == SuggestionType.Calculator)
    {
        // Copy result to clipboard
        try
        {
            Clipboard.SetText(selectedItem.FilePath); // FilePath contains the result
            
            // Show notification
            _notificationText.Text = $"Copied: {selectedItem.FilePath}";
            _notificationText.Visibility = Visibility.Visible;
            
            // Auto-hide notification after 2 seconds
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) =>
            {
                _notificationText.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
            
            // Close overlay
            BeginFadeOutAndClose();
        }
        catch (Exception ex)
        {
            ShowNotification($"Failed to copy: {ex.Message}", NotificationType.Error);
        }
    }
    else if (selectedItem.Type == SuggestionType.WebSearch)
    {
        string queryToSearch = selectedItem.DisplayText.StartsWith("Search Google for")
            ? selectedItem.FilePath
            : selectedItem.DisplayText;

        ExecuteWebSearch(queryToSearch);
        BeginFadeOutAndClose();
    }
}
```

---

## 🎨 **XAML UPDATE (Optional - Better Visual):**

### **Update SuggestionTypeToStringConverter**

**File:** `SearchOverlay.xaml.cs` (Line ~47-62)

```csharp
public class SuggestionTypeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SuggestionType type)
        {
            return type switch
            {
                SuggestionType.App => "Aplikasi Desktop",
                SuggestionType.Calculator => "Calculator",
                SuggestionType.WebSearch => "Pencarian Web",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

---

## ✅ **TESTING:**

### **Test Cases:**

1. **Basic Math:**
   - Input: `2+2` → Result: `= 4`
   - Input: `2 + 2 * 4` → Result: `= 10`
   - Input: `(2+3)*4` → Result: `= 20`

2. **Functions:**
   - Input: `sqrt(16)` → Result: `= 4`
   - Input: `abs(-5)` → Result: `= 5`
   - Input: `round(3.7)` → Result: `= 4`

3. **Constants:**
   - Input: `pi` → Result: `= 3.14159...`
   - Input: `2*pi` → Result: `= 6.28318...`

4. **Power:**
   - Input: `2^3` → Result: `= 8`
   - Input: `10^2` → Result: `= 100`

5. **Fuzzy Match:**
   - Input: `chrome` → Matches: "Google Chrome", "Chrome Remote Desktop"
   - Input: `code` → Matches: "Visual Studio Code", "VS Code"

---

## 📊 **BEFORE vs AFTER:**

### **BEFORE:**
```
User types: "2+2*4"
Result: Search Google for "2+2*4" ❌
```

### **AFTER:**
```
User types: "2+2*4"
Result:
  🧮 = 10                    ← Calculator result (Press Enter to copy)
  🔍 Search Google for "2+2*4"  ← Fallback option
```

---

## 🎯 **SUMMARY:**

**Files Modified:**
1. ✅ `MathEvaluator.cs` - NEW FILE (already created)
2. ⏳ `SearchOverlay.xaml.cs` - Need manual integration (follow steps above)

**Total Code Added:** ~150 lines
**Size Impact:** ~5 KB
**Dependencies:** ZERO (Pure C#)

**Features Added:**
- ✅ Inline calculator (like Spotlight)
- ✅ Fuzzy matching (better search)
- ✅ Copy result to clipboard
- ✅ Smart math detection

---

**Ready to integrate?** Follow the steps above or let me know if you want me to create a complete new SearchOverlay.xaml.cs file! 🚀
