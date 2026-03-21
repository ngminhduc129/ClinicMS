# 🚀 ClinicMS Theme System - Quick Reference Card

## File Locations
```
Resources/Themes/
├── ThemeColors.xaml          # All colors & brushes
├── ThemeTypography.xaml      # Font sizes & text styles
├── ThemeControls.xaml        # Buttons, inputs, datagrids
├── ThemeContainers.xaml      # Cards, layouts, dialogs
├── ClinicTheme.xaml          # Master theme (imported in App.xaml)
├── THEME_GUIDE.md            # Full documentation
└── PAGE_TEMPLATE.xaml        # Template for new pages
```

---

## Most Used Resources

### 🎨 Colors
```xaml
{StaticResource PrimaryGreenBrush}           <!-- Main brand color -->
{StaticResource SurfaceBrushWhite}           <!-- Card/dialog backgrounds -->
{StaticResource BackgroundBrushLight}        <!-- Page background -->
{StaticResource TextBrushPrimary}            <!-- Main text -->
{StaticResource TextBrushSecondary}          <!-- Secondary text -->
{StaticResource SuccessBrush}                <!-- Success status -->
{StaticResource ErrorBrush}                  <!-- Error status -->
{StaticResource WarningBrush}                <!-- Warning status -->
{StaticResource BorderBrushLight}            <!-- Borders -->
```

### 📝 Text Styles
```xaml
{StaticResource PageTitleStyle}              <!-- 28px bold - page headers -->
{StaticResource SectionTitleStyle}           <!-- 20px bold - section headers -->
{StaticResource CardTitleStyle}              <!-- 16px bold - card titles -->
{StaticResource BodyTextStyle}               <!-- 14px regular - body text -->
{StaticResource SecondaryTextStyle}          <!-- 14px regular - secondary text -->
{StaticResource SmallTextStyle}              <!-- 12px regular - helper text -->
{StaticResource FormLabelStyle}              <!-- 13px medium - form labels -->
{StaticResource HelperTextStyle}             <!-- 12px regular - hint text -->
```

### 🔘 Button Styles
```xaml
{StaticResource PrimaryButtonStyle}          <!-- Green CTA button -->
{StaticResource SecondaryButtonStyle}        <!-- White outline button -->
{StaticResource DangerButtonStyle}           <!-- Red delete button -->
{StaticResource IconButtonStyle}             <!-- Compact icon button -->
```

### 📊 Controls
```xaml
{StaticResource ModernTextBoxStyle}          <!-- Modern text input -->
{StaticResource ModernComboBoxStyle}         <!-- Modern dropdown -->
{StaticResource ModernComboBoxItemStyle}     <!-- Dropdown items -->
{StaticResource ModernDataGridStyle}         <!-- Table with green highlights -->
{StaticResource ModernDataGridColumnHeaderStyle}
{StaticResource ModernDataGridRowStyle}
{StaticResource ModernDataGridCellStyle}
```

### 📦 Containers
```xaml
{StaticResource CardContainerStyle}          <!-- Standard card (20px padding) -->
{StaticResource CardContainerCompactStyle}   <!-- Small card (16px padding) -->
{StaticResource CardContainerElevatedStyle}  <!-- Card with large shadow -->
{StaticResource StatCardStyle}               <!-- Dashboard metric card -->
{StaticResource PageContainerStyle}          <!-- Scrollable page container -->
{StaticResource PageInnerContainerStyle}     <!-- Inner content wrapper -->
```

### 🧭 Navigation & Layout
```xaml
{StaticResource SidebarContainerStyle}       <!-- Left sidebar (220px width) -->
{StaticResource SidebarHeaderStyle}          <!-- Sidebar header/logo area -->
{StaticResource SidebarItemButtonStyle}      <!-- Sidebar nav button -->
{StaticResource FormContainerStyle}          <!-- Form wrapper (16px spacing) -->
{StaticResource FormFieldStyle}              <!-- Label + input wrapper -->
{StaticResource FormRowStyle}                <!-- Multi-column form row -->
```

### 🪟 Dialogs & Modals
```xaml
{StaticResource ModalOverlayStyle}           <!-- Semi-transparent overlay -->
{StaticResource DialogContainerStyle}        <!-- Dialog box -->
{StaticResource DialogHeaderStyle}           <!-- Dialog title bar -->
{StaticResource DialogTitleStyle}            <!-- Dialog heading text -->
{StaticResource DialogContentStyle}          <!-- Dialog scrollable content -->
{StaticResource DialogFooterStyle}           <!-- Dialog button bar -->
{StaticResource DialogFooterButtonsStyle}    <!-- Footer button container -->
```

### 🏷️ Badges & Status
```xaml
{StaticResource StatusBadgeSuccessStyle}     <!-- Green success badge -->
{StaticResource StatusBadgeWarningStyle}     <!-- Amber warning badge -->
{StaticResource StatusBadgeErrorStyle}       <!-- Red error badge -->
{StaticResource StatusBadgeInfoStyle}        <!-- Blue info badge -->
{StaticResource StatusBadgeLabelStyle}       <!-- Badge text style -->
```

### 🎭 Effects
```xaml
{StaticResource StandardShadow}              <!-- Normal elevation shadow -->
{StaticResource ElevatedShadow}              <!-- High elevation shadow -->
```

---

## Common Patterns

### Page Template
```xaml
<UserControl>
    <ScrollViewer Style="{StaticResource PageContainerStyle}">
        <StackPanel Style="{StaticResource PageInnerContainerStyle}">
            <!-- Title -->
            <TextBlock Style="{StaticResource PageTitleStyle}" Text="Page Title" />
            
            <!-- Content -->
            <Border Style="{StaticResource CardContainerStyle}">
                <!-- content -->
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

### Form Pattern
```xaml
<StackPanel Style="{StaticResource FormContainerStyle}">
    <StackPanel Style="{StaticResource FormFieldStyle}">
        <TextBlock Style="{StaticResource FormLabelStyle}" Text="Label:" />
        <TextBox Style="{StaticResource ModernTextBoxStyle}" />
    </StackPanel>
</StackPanel>
```

### Dialog Pattern
```xaml
<Window Style="{StaticResource DialogWindowStyle}" Width="500" Height="600">
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
                <!-- content -->
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

### DataGrid Pattern
```xaml
<DataGrid Style="{StaticResource ModernDataGridStyle}"
          ColumnHeaderStyle="{StaticResource ModernDataGridColumnHeaderStyle}"
          RowStyle="{StaticResource ModernDataGridRowStyle}"
          CellStyle="{StaticResource ModernDataGridCellStyle}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*" />
        <DataGridTextColumn Header="Status" Binding="{Binding Status}" Width="100" />
    </DataGrid.Columns>
</DataGrid>
```

### Two-Column Form Row
```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="12"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <StackPanel Grid.Column="0" Style="{StaticResource FormFieldStyle}">
        <TextBlock Style="{StaticResource FormLabelStyle}" Text="First" />
        <TextBox Style="{StaticResource ModernTextBoxStyle}" />
    </StackPanel>
    
    <StackPanel Grid.Column="2" Style="{StaticResource FormFieldStyle}">
        <TextBlock Style="{StaticResource FormLabelStyle}" Text="Second" />
        <TextBox Style="{StaticResource ModernTextBoxStyle}" />
    </StackPanel>
</Grid>
```

### Stat Card (Dashboard)
```xaml
<Border Style="{StaticResource StatCardStyle}">
    <StackPanel VerticalAlignment="Center">
        <TextBlock Style="{StaticResource SmallTextStyle}" Text="Metric" />
        <TextBlock Text="42" FontSize="32" FontWeight="Bold" 
                   Foreground="{StaticResource PrimaryGreenBrush}" />
    </StackPanel>
</Border>
```

### Sidebar Navigation
```xaml
<StackPanel Style="{StaticResource SidebarContainerStyle}">
    <Border Style="{StaticResource SidebarHeaderStyle}">
        <TextBlock Style="{StaticResource SidebarLogoStyle}" Text="ClinicMS" />
    </Border>
    
    <Button Style="{StaticResource SidebarItemButtonStyle}" 
            Tag="active" Content="Active" />
    <Button Style="{StaticResource SidebarItemButtonStyle}" 
            Content="Inactive" />
</StackPanel>
```

---

## Color Hex Reference

| Color Name | Hex | Usage |
|-----------|-----|-------|
| Primary Green | #16A34A | Buttons, highlights, active states |
| Primary Green Dark | #166534 | Hover states, sidebar |
| Primary Green Light | #DCFCE7 | Light highlights, selections |
| Success | #10B981 | Success badges, checkmarks |
| Warning | #F59E0B | Warning badges, alerts |
| Error | #EF4444 | Error badges, delete buttons |
| Info | #3B82F6 | Info badges, notifications |
| Text Primary | #111827 | Main text, headings |
| Text Secondary | #6B7280 | Secondary text, hints |
| Border Light | #E5E7EB | Subtle borders, dividers |
| Surface White | #FFFFFF | Cards, dialogs, panels |
| Background Light | #F9FAFB | Page background |
| Header Background | #F3F4F6 | Dialog headers |

---

## Font Sizes

| Size | Pixels | Usage |
|------|--------|-------|
| Page Title | 28px | Main page heading |
| Section Title | 20px | Section headings |
| Subsection Title | 18px | Subsection headings |
| Card Title | 16px | Card headings |
| Body | 14px | Normal text |
| Label | 13px | Form labels |
| Small | 12px | Helper text, secondary |
| X-Small | 11px | Sidebar labels |

---

## Spacing

| Size | Usage |
|------|-------|
| 6px | Tight spacing (label-input) |
| 12px | Normal spacing (form rows) |
| 16px | Form section spacing |
| 20px | Card padding (standard) |
| 24px | Page padding |

---

## Keyboard Shortcuts

When implementing keyboard shortcuts:
- **ESC** - Close dialogs
- **Enter** - Submit forms
- **Tab** - Navigate inputs (automatic)

---

## Customization Checklist

To change the entire theme:

- [ ] Edit `ThemeColors.xaml` - Change `PrimaryGreenColor`
- [ ] Update `SidebarContainerStyle` background color
- [ ] Modify button hover colors in `ThemeControls.xaml`
- [ ] Adjust spacing in card styles if needed
- [ ] Update accent colors in status badges
- [ ] Test on all pages (Patient, Doctor, Visit, etc.)

To add a new color:

1. Add to `ThemeColors.xaml`:
   ```xaml
   <Color x:Key="MyNewColor">#XXXXXX</Color>
   <SolidColorBrush x:Key="MyNewBrush" Color="{StaticResource MyNewColor}"/>
   ```

2. Use in XAML:
   ```xaml
   Foreground="{StaticResource MyNewBrush}"
   ```

---

## Troubleshooting

### Styles not applying?
- Check spelling of style name in `{StaticResource}`
- Ensure theme files are in `Resources/Themes/` folder
- Verify `App.xaml` includes `ClinicTheme.xaml` in MergedDictionaries
- Clean and rebuild solution

### Colors look different?
- Check if you're using a color hex instead of a brush resource
- Verify brush opacity is correct
- Ensure you're using the right brush for the context

### Dialog doesn't look right?
- Make sure parent window is styled with `DialogWindowStyle`
- Check dialog is inside a `Border` with `DialogContainerStyle`
- Verify content is in 3-row grid (Header/Content/Footer)

### Text is hard to read?
- Use `TextBrushPrimary` for main text
- Use `TextBrushSecondary` for secondary text
- Ensure sufficient contrast (at least 4.5:1 ratio)

---

## Resources

- 📖 Full Guide: `THEME_GUIDE.md`
- 🎨 Color System: `ThemeColors.xaml`
- 📝 Typography: `ThemeTypography.xaml`
- 🔘 Controls: `ThemeControls.xaml`
- 📦 Layouts: `ThemeContainers.xaml`
- 📋 Page Template: `PAGE_TEMPLATE.xaml`
- 🎯 Master Theme: `ClinicTheme.xaml` (main import)

---

## Pro Tips

1. **Always use StaticResource** - Never hard-code colors
2. **Copy PAGE_TEMPLATE.xaml** - Don't start from scratch
3. **Use Spacing consistently** - Stick to 6/12/16/20/24px
4. **Test Dark mode ready** - Use resource brushes for future themes
5. **Keep Dialog width max 600px** - Easier to read on smaller screens
6. **Use Icon buttons sparingly** - Add tooltips for clarity
7. **Consistent button sizing** - All primary buttons 40px height
8. **Form validation styling** - Use border color change for errors

