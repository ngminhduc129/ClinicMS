# ✅ Theme Migration Checklist - Convert Pages to Modern Theme

Use this checklist to systematically update existing XAML pages to use the new theme system.

---

## 📋 Pre-Migration

- [ ] Read `THEME_GUIDE.md` for overview
- [ ] Review `QUICK_REFERENCE.md` for style names
- [ ] Keep current page open for reference
- [ ] Have `PAGE_TEMPLATE.xaml` open in another window
- [ ] Backup current page XAML (optional)

---

## Phase 1: Page Container & Structure

- [ ] Replace `<Grid>` root with proper page structure
- [ ] Add `<ScrollViewer Style="{StaticResource PageContainerStyle}">`
- [ ] Wrap content in `<StackPanel Style="{StaticResource PageInnerContainerStyle}">`
- [ ] Remove manual margin/padding settings
- [ ] Verify page background is now light gray (#F9FAFB)

### Before:
```xaml
<UserControl>
    <Grid Background="White">
        <StackPanel Margin="20" Padding="20">
```

### After:
```xaml
<UserControl>
    <ScrollViewer Style="{StaticResource PageContainerStyle}">
        <StackPanel Style="{StaticResource PageInnerContainerStyle}">
```

---

## Phase 2: Page Title & Header

- [ ] Replace page title `<TextBlock>` with PageTitleStyle
- [ ] Remove manual color/font settings
- [ ] Add subtitle if description exists
- [ ] Position action button in header grid

### Before:
```xaml
<TextBlock Text="Patients" FontSize="24" FontWeight="Bold" 
           Foreground="#1F2937" Margin="0,0,0,20" />
<Button Content="Add" Background="#16A34A" Foreground="White" />
```

### After:
```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    
    <TextBlock Grid.Column="0" Style="{StaticResource PageTitleStyle}" Text="Patients" />
    <Button Grid.Column="1" Style="{StaticResource PrimaryButtonStyle}" Content="+ Add Patient" />
</Grid>
```

---

## Phase 3: Search/Filter Card

- [ ] Create search card with `CardContainerCompactStyle`
- [ ] Replace TextBox with `ModernTextBoxStyle`
- [ ] Update buttons to use `SecondaryButtonStyle`
- [ ] Remove borders and background colors manually

### Before:
```xaml
<Border Background="White" BorderThickness="1" BorderBrush="#E5E7EB">
    <TextBox Padding="8" Margin="10" />
</Border>
```

### After:
```xaml
<Border Style="{StaticResource CardContainerCompactStyle}">
    <Grid Spacing="12">
        <TextBox Style="{StaticResource ModernTextBoxStyle}" />
        <Button Style="{StaticResource SecondaryButtonStyle}" />
    </Grid>
</Border>
```

---

## Phase 4: DataGrid Styling

- [ ] Add `Style="{StaticResource ModernDataGridStyle}"` to DataGrid
- [ ] Add column header style: `ColumnHeaderStyle="{StaticResource ModernDataGridColumnHeaderStyle}"`
- [ ] Add row style: `RowStyle="{StaticResource ModernDataGridRowStyle}"`
- [ ] Add cell style: `CellStyle="{StaticResource ModernDataGridCellStyle}"`
- [ ] Wrap DataGrid in card: `<Border Style="{StaticResource CardContainerStyle}">`
- [ ] Remove manual row colors and hover effects
- [ ] Verify green row highlighting on hover/selection

### Before:
```xaml
<Border Background="White" BorderThickness="1">
    <DataGrid AutoGenerateColumns="False">
```

### After:
```xaml
<Border Style="{StaticResource CardContainerStyle}">
    <DataGrid Style="{StaticResource ModernDataGridStyle}"
              ColumnHeaderStyle="{StaticResource ModernDataGridColumnHeaderStyle}"
              RowStyle="{StaticResource ModernDataGridRowStyle}"
              CellStyle="{StaticResource ModernDataGridCellStyle}"
              AutoGenerateColumns="False">
```

---

## Phase 5: Buttons

- [ ] Find all `<Button>` elements
- [ ] Replace primary buttons (green) with `PrimaryButtonStyle`
- [ ] Replace secondary buttons (outline) with `SecondaryButtonStyle`
- [ ] Replace delete buttons (red) with `DangerButtonStyle`
- [ ] Replace icon-only buttons with `IconButtonStyle`
- [ ] Remove manual colors and sizing

### Mapping:
```
❌ Hard-coded Green           → ✅ PrimaryButtonStyle
❌ White with border          → ✅ SecondaryButtonStyle
❌ Red DeleteButton / DangerButton → ✅ DangerButtonStyle
❌ Small compact icon button   → ✅ IconButtonStyle
```

### Before:
```xaml
<Button Background="#16A34A" Foreground="White" Padding="16,8" Content="Save" />
```

### After:
```xaml
<Button Style="{StaticResource PrimaryButtonStyle}" Content="Save" />
```

---

## Phase 6: Text Inputs

- [ ] Find all `<TextBox>` elements
- [ ] Apply `Style="{StaticResource ModernTextBoxStyle}"`
- [ ] Remove manual colors, borders, padding
- [ ] Verify green border appears on focus

### Before:
```xaml
<TextBox Background="White" Foreground="#1F2937" Padding="8" 
         BorderThickness="1" BorderBrush="#E5E7EB" />
```

### After:
```xaml
<TextBox Style="{StaticResource ModernTextBoxStyle}" />
```

---

## Phase 7: ComboBoxes

- [ ] Find all `<ComboBox>` elements
- [ ] Apply `Style="{StaticResource ModernComboBoxStyle}"`
- [ ] Apply `Style="{StaticResource ModernComboBoxItemStyle}"` to each `<ComboBoxItem>`
- [ ] Remove manual styling

### Before:
```xaml
<ComboBox Background="White" Foreground="#1F2937">
    <ComboBoxItem />
</ComboBox>
```

### After:
```xaml
<ComboBox Style="{StaticResource ModernComboBoxStyle}">
    <ComboBoxItem Style="{StaticResource ModernComboBoxItemStyle}" />
</ComboBox>
```

---

## Phase 8: Text Styling

- [ ] Find all text elements (`<TextBlock>`)
- [ ] Replace titles with appropriate style:
  - 28px bold → `PageTitleStyle`
  - 20px bold → `SectionTitleStyle`
  - 16px bold → `CardTitleStyle`
  - 14px regular → `BodyTextStyle`
  - 12px secondary → `SmallTextStyle`
- [ ] Remove manual FontSize, FontWeight, Foreground
- [ ] Verify text hierarchy is correct

### Before:
```xaml
<TextBlock Text="Title" FontSize="24" FontWeight="Bold" Foreground="#1F2937" />
```

### After:
```xaml
<TextBlock Style="{StaticResource PageTitleStyle}" Text="Title" />
```

---

## Phase 9: Colors & Brushes

- [ ] Find all hard-coded colors (like `#1F2937`)
- [ ] Replace with resource brushes:
  - `#1F2937` → `{StaticResource TextBrushPrimary}`
  - `#6B7280` → `{StaticResource TextBrushSecondary}`
  - `#E5E7EB` → `{StaticResource BorderBrushLight}`
  - `#FFFFFF` → `{StaticResource SurfaceBrushWhite}`
  - `#F9FAFB` → `{StaticResource BackgroundBrushLight}`
- [ ] Use `Foreground="{StaticResource ...}"` for text colors
- [ ] Use `Background="{StaticResource ...}"` for backgrounds
- [ ] Use `BorderBrush="{StaticResource ...}"` for borders

### Before:
```xaml
<TextBlock Foreground="#1F2937" />
<Border Background="#FFFFFF" BorderBrush="#E5E7EB" />
```

### After:
```xaml
<TextBlock Foreground="{StaticResource TextBrushPrimary}" />
<Border Background="{StaticResource SurfaceBrushWhite}" 
        BorderBrush="{StaticResource BorderBrushLight}" />
```

---

## Phase 10: Cards & Containers

- [ ] Wrap content sections in card styles
- [ ] Change `<Border Background="White" ...>` to `<Border Style="{StaticResource CardContainerStyle}">`
- [ ] Use `CardContainerCompactStyle` for smaller cards
- [ ] Use `CardContainerElevatedStyle` for important cards
- [ ] Remove manual borders, shadows, padding

### Before:
```xaml
<Border Background="White" BorderThickness="1" BorderBrush="#E5E7EB" Padding="20">
    <StackPanel Margin="0,0,0,20" Spacing="10">
```

### After:
```xaml
<Border Style="{StaticResource CardContainerStyle}">
    <StackPanel Style="{StaticResource FormContainerStyle}">
```

---

## Phase 11: Forms

- [ ] Wrap entire form in `<StackPanel Style="{StaticResource FormContainerStyle}">`
- [ ] Wrap each label+input in `<StackPanel Style="{StaticResource FormFieldStyle}"`
- [ ] For multi-column rows, use `<Grid Style="{StaticResource FormRowStyle}">`
- [ ] Use `FormLabelStyle` for all labels
- [ ] Add `HelperTextStyle` hints below inputs

### Before:
```xaml
<StackPanel Margin="0,0,0,20" Spacing="10">
    <TextBlock Text="Name:" FontWeight="Bold" Margin="0,0,0,5" />
    <TextBox Padding="8" />
</StackPanel>
```

### After:
```xaml
<StackPanel Style="{StaticResource FormContainerStyle}">
    <StackPanel Style="{StaticResource FormFieldStyle}">
        <TextBlock Style="{StaticResource FormLabelStyle}" Text="Name:" />
        <TextBox Style="{StaticResource ModernTextBoxStyle}" />
        <TextBlock Style="{StaticResource HelperTextStyle}" Text="Your full name" />
    </StackPanel>
</StackPanel>
```

---

## Phase 12: Dialogs

- [ ] Update dialog window to use `Style="{StaticResource DialogWindowStyle}"`
- [ ] Wrap content in `<Border Style="{StaticResource DialogContainerStyle}">`
- [ ] Create 3-row grid: Header (60px) / Content (auto) / Footer (60px)
- [ ] Apply header style: `<Grid Style="{StaticResource DialogHeaderStyle}">`
- [ ] Apply content style: `<ScrollViewer Style="{StaticResource DialogContentStyle}">`
- [ ] Apply footer style: `<Grid Style="{StaticResource DialogFooterStyle}">`
- [ ] Wrap footer buttons in `<StackPanel Style="{StaticResource DialogFooterButtonsStyle}">`

### Before:
```xaml
<Window Background="White">
    <Grid>
        <TextBlock Text="Title" FontSize="18" Margin="10" />
```

### After:
```xaml
<Window Style="{StaticResource DialogWindowStyle}">
    <Border Style="{StaticResource DialogContainerStyle}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="60"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="60"/>
            </Grid.RowDefinitions>
            
            <Grid Grid.Row="0" Style="{StaticResource DialogHeaderStyle}">
                <TextBlock Style="{StaticResource DialogTitleStyle}" Text="Title" />
            </Grid>
            
            <ScrollViewer Grid.Row="1" Style="{StaticResource DialogContentStyle}">
                <!-- Form content -->
            </ScrollViewer>
            
            <Grid Grid.Row="2" Style="{StaticResource DialogFooterStyle}">
                <StackPanel Style="{StaticResource DialogFooterButtonsStyle}">
                    <Button Style="{StaticResource SecondaryButtonStyle}" Content="Cancel" />
                    <Button Style="{StaticResource PrimaryButtonStyle}" Content="Save" />
                </StackPanel>
            </Grid>
        </Grid>
    </Border>
</Window>
```

---

## Phase 13: Status Indicators

- [ ] Find status display elements (badges, indicators)
- [ ] Replace with appropriate badge style:
  - Success → `StatusBadgeSuccessStyle`
  - Warning → `StatusBadgeWarningStyle`
  - Error → `StatusBadgeErrorStyle`
  - Info → `StatusBadgeInfoStyle`
- [ ] Use `StatusBadgeLabelStyle` for text inside badges
- [ ] Add appropriate color brush

### Before:
```xaml
<Border Background="#F0FDF4" BorderBrush="#10B981">
    <TextBlock Text="Active" Foreground="#10B981" />
</Border>
```

### After:
```xaml
<Border Style="{StaticResource StatusBadgeSuccessStyle}">
    <TextBlock Style="{StaticResource StatusBadgeLabelStyle}" 
               Foreground="{StaticResource SuccessBrush}"
               Text="Active" />
</Border>
```

---

## Phase 14: Separators & Spacing

- [ ] Add dividers between sections: `<Border Style="{StaticResource HorizontalDividerStyle}" />`
- [ ] Remove manual margin/padding - rely on StackPanel Spacing
- [ ] Use consistent spacing: 6px (tight), 12px (normal), 16px (section), 24px (page)
- [ ] Never use hardcoded margins - use Margin from container styles

### Before:
```xaml
<StackPanel Margin="0,20,0,0" Spacing="0">
    <TextBlock />
    <Border Background="#E5E7EB" Height="1" Margin="0,20,0,20" />
    <TextBlock />
</StackPanel>
```

### After:
```xaml
<StackPanel Style="{StaticResource FormContainerStyle}">
    <TextBlock />
    <Border Style="{StaticResource HorizontalDividerStyle}" />
    <TextBlock />
</StackPanel>
```

---

## Phase 15: Effects & Shadows

- [ ] If element needs shadow:
  - Add `Effect="{StaticResource StandardShadow}"` for normal cards
  - Add `Effect="{StaticResource ElevatedShadow}"` for modals/important items
- [ ] Remove manual DropShadowEffect definitions
- [ ] Verify shadows align with elevation model

### Before:
```xaml
<Border Background="White">
    <Border.Effect>
        <DropShadowEffect BlurRadius="5" ShadowDepth="2" />
    </Border.Effect>
</Border>
```

### After:
```xaml
<Border Style="{StaticResource CardContainerStyle}">
    <!-- Shadow already included -->
</Border>
```

---

## Post-Migration Verification

After updating each page:

### Visual Checks
- [ ] Page loads without XAML errors
- [ ] Colors look correct (green brand, proper text contrast)
- [ ] Spacing is consistent (no weird gaps)
- [ ] Text hierarchy is clear (titles > headings > body)
- [ ] Buttons have green color
- [ ] Forms have proper field grouping
- [ ] DataGrid rows highlight on hover
- [ ] Dialogs are centered with proper structure

### Functional Checks
- [ ] Search/filter still works
- [ ] Buttons are clickable
- [ ] DataGrid sorting works
- [ ] Forms can be submitted
- [ ] Dialogs can be closed
- [ ] No error messages in output

### Browser Check
- [ ] Page renders at 1024x768
- [ ] Page renders at 1920x1080
- [ ] Scrolling works properly
- [ ] No horizontal scrollbars needed

---

## Troubleshooting During Migration

### Problem: Styles not applying
- **Solution:** Verify style name spelling in `{StaticResource}`
- **Solution:** Check theme files are in `Resources/Themes/` folder
- **Solution:** Verify `App.xaml` includes theme in MergedDictionaries
- **Solution:** Rebuild solution

### Problem: Colors still look wrong
- **Solution:** Search for `Foreground="` or `Background="` hard-coded colors
- **Solution:** Replace with `{StaticResource BrushName}`
- **Solution:** Check color hex values aren't overriding theme

### Problem: Buttons don't look right
- **Solution:** Make sure `Style="{StaticResource PrimaryButtonStyle}"` is applied
- **Solution:** Remove manual `Background`, `Foreground` properties
- **Solution:** Check button isn't inside a Grid that changes colors

### Problem: DataGrid not highlighting
- **Solution:** Verify `RowStyle="{StaticResource ModernDataGridRowStyle}"` is set
- **Solution:** Verify `CellStyle="{StaticResource ModernDataGridCellStyle}"` is set
- **Solution:** Check SelectedItem binding is working

---

## Page Migration Sequence

Recommended order to migrate pages:

1. **PatientListView** - Simple list with filter
2. **PatientFormDialog** - Form with multiple fields
3. **DoctorListView** - Similar to patient list
4. **VisitListView** - Similar structure
5. **DashboardView** - Card-based with stat boxes
6. Other views...

---

## Completion Checklist

Final verification:

- [ ] All 14 phases completed for each page
- [ ] No XAML errors in output
- [ ] All pages render correctly
- [ ] Theme colors applied consistently
- [ ] Typography hierarchy correct
- [ ] Buttons have proper styles
- [ ] Forms look professional
- [ ] Dialogs centered with proper structure
- [ ] DataGrids highlight on hover
- [ ] Search/filter functionality intact
- [ ] All CRUD operations working
- [ ] No performance issues

---

## Success Criteria

When migration is complete, ClinicMS will have:

✅ Professional SaaS dashboard appearance  
✅ Consistent green medical brand throughout  
✅ Cohesive typography and spacing  
✅ Modern button and input styles  
✅ Professional card-based layouts  
✅ Smooth hover and interaction effects  
✅ Accessible color contrasts  
✅ Easy to maintain (change colors in one place)  

---

## References

- **Full Theme Guide:** `THEME_GUIDE.md`
- **Quick Reference:** `QUICK_REFERENCE.md`
- **Page Template:** `PAGE_TEMPLATE.xaml`
- **Theme Files:** `Resources/Themes/` (5 XAML files)

Good luck with the migration! 🎨✨

