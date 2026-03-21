# Modern SaaS Design - Quick Reference Guide

## Color Palette

### Primary Brand
```
Primary Green:        #16A34A
Dark Green (Sidebar):  #14532D
Light Green:          #DCFCE7
Very Light Green:     #F0FDF4
```

### Backgrounds & Surfaces
```
Page Background:      #F6F9FC
Card Background:      #FFFFFF
Header Background:    #F8FAFC
Hover Background:     #F3F4F6
```

### Text Colors
```
Primary Text:         #111827
Secondary Text:       #6B7280
Tertiary Text:        #9CA3AF
Disabled Text:        #9CA3AF
White Text:           #FFFFFF
```

### Borders & Dividers
```
Light Border:         #E5E7EB
Medium Border:        #D1D5DB
Divider:              #E5E7EB
Card Border:          #E5E7EB
```

### Status Colors
```
Success:              #10B981
Warning:              #F59E0B
Error:                #EF4444
Info:                 #3B82F6
```

---

## Corner Radius Sizes

```
XL (Modals/Cards):    CornerRadius="20" or "18"
Large (Buttons):      CornerRadius="12" or "11"
Medium (Inputs):      CornerRadius="10"
Small (Icons):        CornerRadius="8"
```

---

## Shadow Effects

```xaml
<!-- Soft Shadow - Cards -->
<DropShadowEffect x:Key="SoftShadow" 
                  Color="#000000" BlurRadius="12" 
                  ShadowDepth="1" Opacity="0.08" Direction="270"/>

<!-- Standard Shadow - Primary elevation -->
<DropShadowEffect x:Key="StandardShadow" 
                  Color="#000000" BlurRadius="16" 
                  ShadowDepth="2" Opacity="0.12" Direction="270"/>

<!-- Elevated Shadow - Modals -->
<DropShadowEffect x:Key="ElevatedShadow" 
                  Color="#000000" BlurRadius="24" 
                  ShadowDepth="4" Opacity="0.16" Direction="270"/>

<!-- Inset Shadow - Inputs -->
<DropShadowEffect x:Key="InsetShadow" 
                  Color="#000000" BlurRadius="8" 
                  ShadowDepth="1" Opacity="0.06" Direction="270"/>
```

---

## Button Styles

### Primary Button
```xaml
<Button Style="{StaticResource PrimaryButtonStyle}" 
        Content="Action" />
<!-- Green background, white text, 40px height, rounded -->
```

### Secondary Button (Outlined)
```xaml
<Button Style="{StaticResource SecondaryButtonStyle}" 
        Content="Cancel" />
<!-- White background, green border on hover, 40px height -->
```

### Danger Button
```xaml
<Button Style="{StaticResource DangerButtonStyle}" 
        Content="Delete" />
<!-- Red background, white text, 40px height -->
```

### Icon Button (Small)
```xaml
<Button Style="{StaticResource IconButtonStyle}">
  <TextBlock Text="🔍" FontSize="16" />
</Button>
<!-- 40x40 transparent, hover highlight -->
```

---

## Input Fields

### TextBox
```xaml
<TextBox Style="{StaticResource ModernTextBoxStyle}" 
         Placeholder="Enter text..." />
<!-- 40px height, rounded corners, green focus glow -->
```

### ComboBox
```xaml
<ComboBox Style="{StaticResource ModernComboBoxStyle}">
  <ComboBoxItem>Option 1</ComboBoxItem>
</ComboBox>
<!-- Rounded dropdown with shadow -->
```

---

## Container Styles

### Standard Card
```xaml
<Border Style="{StaticResource CardContainerStyle}">
  <!-- Content -->
</Border>
<!-- 20px corner radius, 24px padding, standard shadow -->
```

### Compact Card
```xaml
<Border Style="{StaticResource CardContainerCompactStyle}">
  <!-- Content -->
</Border>
<!-- 16px corner radius, 20px padding, less padding -->
```

### Elevated Card (Modal)
```xaml
<Border Style="{StaticResource CardContainerElevatedStyle}">
  <!-- Content -->
</Border>
<!-- 20px corner radius, elevated shadow -->
```

### Stat Card (Dashboard)
```xaml
<Border Style="{StaticResource StatCardStyle}">
  <!-- KPI Content -->
</Border>
<!-- 18px corners, 160px height, standard shadow -->
```

---

## Typography

### Sizes
```
Page Title:           32px | ExtraBold
Section Title:        20px | Bold
Card Title:           16px | SemiBold
Body Text:            14px | Regular
Small Text:           12px | Regular
Helper Text:          11px | Regular
```

### Implementation
```xaml
<!-- Page Title -->
<TextBlock Text="Bệnh Nhân" 
           FontSize="32" 
           FontWeight="ExtraBold" />

<!-- Body Text -->
<TextBlock Text="Description" 
           FontSize="14" 
           Foreground="#6B7280" />

<!-- Small Text -->
<TextBlock Text="Helper" 
           FontSize="12" 
           Foreground="#9CA3AF" />
```

---

## Layout Spacing

```
Page/Section Margin:     40px
Card Padding:            24px
Element Gaps:            16px
Compact Gaps:            12px
Tight Gaps:              8px
Line Height:             1.5
```

### Example Page Structure
```xaml
<Grid Margin="40">
  <!-- Title: 32px margin-bottom -->
  <TextBlock Text="Title" Margin="0,0,0,32" />
  
  <!-- Subtitle: 8px margin-top -->
  <TextBlock Text="Subtitle" Margin="0,8,0,0" />
  
  <!-- Toolbar: 32px margin-top -->
  <Grid Margin="0,32,0,0" />
  
  <!-- Table Card: 24px margin-top -->
  <Border Style="{StaticResource CardContainerStyle}" 
          Margin="0,24,0,0" />
</Grid>
```

---

## Hover Animations

### Smooth Transition (200ms)
```xaml
<EventTrigger RoutedEvent="Button.MouseEnter">
  <BeginStoryboard>
    <Storyboard>
      <DoubleAnimation Storyboard.TargetName="HoverOverlay" 
                     Storyboard.TargetProperty="Opacity" 
                     To="1" Duration="0:0:0.2"/>
    </Storyboard>
  </BeginStoryboard>
</EventTrigger>
```

### Standard Timings
```
Quick feedback:   150ms (0:0:0.15)
Standard:         200ms (0:0:0.2)
Smooth:           300ms (0:0:0.3)
```

---

## DataGrid Styling

```xaml
<DataGrid.ColumnHeaderStyle>
  <Style TargetType="DataGridColumnHeader">
    <Setter Property="Background" Value="#F8FAFC"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Padding" Value="18,16"/>
  </Style>
</DataGrid.ColumnHeaderStyle>

<DataGrid.RowStyle>
  <Style TargetType="DataGridRow">
    <Setter Property="Height" Value="56"/>
    <Setter Property="BorderBrush" Value="#F1F5F9"/>
    <Setter Property="BorderThickness" Value="0,0,0,1"/>
    <Style.Triggers>
      <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="Background" Value="#F0FDF4"/>
      </Trigger>
    </Style.Triggers>
  </Style>
</DataGrid.RowStyle>
```

---

## Form Field Layout

```xaml
<StackPanel Orientation="Vertical" Margin="0,0,0,16">
  <TextBlock Text="Label" FontWeight="SemiBold" Margin="0,0,0,6"/>
  <TextBox Style="{StaticResource ModernTextBoxStyle}"/>
</StackPanel>

<!-- Multiple fields side-by-side -->
<Grid Margin="0,0,0,16">
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="16"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  
  <StackPanel Grid.Column="0">
    <TextBlock Text="Field 1" FontWeight="SemiBold" Margin="0,0,0,6"/>
    <TextBox Style="{StaticResource ModernTextBoxStyle}"/>
  </StackPanel>
  
  <StackPanel Grid.Column="2">
    <TextBlock Text="Field 2" FontWeight="SemiBold" Margin="0,0,0,6"/>
    <TextBox Style="{StaticResource ModernTextBoxStyle}"/>
  </StackPanel>
</Grid>
```

---

## Search Bar

```xaml
<Border Background="White" CornerRadius="12" 
        BorderThickness="1.5" BorderBrush="#E5E7EB"
        Width="360" Height="42">
  <Border.Effect>
    <DropShadowEffect Color="#000000" BlurRadius="12" 
                    ShadowDepth="1" Opacity="0.08" Direction="270"/>
  </Border.Effect>
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <TextBlock Text="🔍" Margin="14,0,12,0" VerticalAlignment="Center"/>
    <TextBox Grid.Column="1" BorderThickness="0" 
           Background="Transparent" Padding="0"/>
  </Grid>
</Border>
```

---

## Empty State

```xaml
<StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
  <TextBlock Text="📋" FontSize="56" 
           HorizontalAlignment="Center" Opacity="0.3"/>
  
  <TextBlock Text="No items found" 
           FontSize="17" FontWeight="SemiBold" 
           Foreground="#475569" 
           Margin="0,16,0,8"/>
  
  <TextBlock Text="Try adjusting your search filters." 
           FontSize="13" Foreground="#94A3B8"/>
</StackPanel>
```

---

## Table Action Buttons

```xaml
<Button Width="36" Height="36" Background="Transparent" 
        BorderThickness="0" ToolTip="View">
  <Button.Template>
    <ControlTemplate TargetType="Button">
      <Border x:Name="Bg" CornerRadius="8" Background="Transparent">
        <TextBlock x:Name="Icon" Text="👁" FontSize="15" 
                 HorizontalAlignment="Center" 
                 VerticalAlignment="Center" Opacity="0.6"/>
      </Border>
      <ControlTemplate.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
          <Setter TargetName="Bg" Property="Background" Value="#DCFCE7"/>
          <Setter TargetName="Icon" Property="Opacity" Value="1"/>
        </Trigger>
      </ControlTemplate.Triggers>
    </ControlTemplate>
  </Button.Template>
</Button>
```

---

## Focus & Accessibility

### Focus State (Inputs)
```xaml
<Trigger Property="IsFocused" Value="True">
  <Setter Property="BorderBrush" Value="#16A34A"/>
  <Setter Property="BorderThickness" Value="2"/>
  <Setter Property="Effect">
    <Setter.Value>
      <DropShadowEffect Color="#16A34A" BlurRadius="12" 
                      ShadowDepth="2" Opacity="0.25" Direction="270"/>
    </Setter.Value>
  </Setter>
</Trigger>
```

### Disabled State
```xaml
<Trigger Property="IsEnabled" Value="False">
  <Setter Property="Background" Value="#F3F4F6"/>
  <Setter Property="Foreground" Value="#9CA3AF"/>
  <Setter Property="BorderBrush" Value="#E5E7EB"/>
  <Setter Property="Opacity" Value="0.5"/>
</Trigger>
```

---

## Responsive Breakpoints

```
Minimum Width:        900px
Typical Desktop:      1280px
Large Desktop:        1400px+
Max Content Width:    1280px
```

---

## Resource Usage

### Define Once, Use Everywhere
```xaml
<!-- In ThemeColors.xaml -->
<SolidColorBrush x:Key="PrimaryGreenBrush" 
                 Color="#16A34A"/>

<!-- In any XAML file -->
<Button Background="{StaticResource PrimaryGreenBrush}" />
```

### Shadow Resource
```xaml
<!-- In ThemeColors.xaml -->
<DropShadowEffect x:Key="StandardShadow" 
                 Color="#000000" BlurRadius="16" 
                 ShadowDepth="2" Opacity="0.12"/>

<!-- In Container -->
<Border Effect="{StaticResource StandardShadow}" />
```

---

## Common Code Snippets

### Modern Button Row
```xaml
<StackPanel Orientation="Horizontal" Margin="0,24,0,0">
  <Button Style="{StaticResource PrimaryButtonStyle}" 
         Content="Save" Padding="20,0" Width="Auto"/>
  <Button Style="{StaticResource SecondaryButtonStyle}" 
         Content="Cancel" Padding="20,0" Width="Auto" Margin="12,0,0,0"/>
</StackPanel>
```

### Modern Card Container
```xaml
<Border Background="White" CornerRadius="20" Padding="24">
  <Border.Effect>
    <DropShadowEffect Color="#000000" BlurRadius="16" 
                    ShadowDepth="2" Opacity="0.12" Direction="270"/>
  </Border.Effect>
  <!-- Card content -->
</Border>
```

### Modern Section with Title
```xaml
<Grid Margin="40">
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  
  <TextBlock Text="Section" FontSize="32" FontWeight="ExtraBold"/>
  <TextBlock Grid.Row="1" Text="Subtitle" FontSize="14" 
           Foreground="#6B7280" Margin="0,8,0,0"/>
  
  <Border Grid.Row="2" Style="{StaticResource CardContainerStyle}" 
         Margin="0,32,0,0">
    <!-- Content -->
  </Border>
</Grid>
```

---

## Troubleshooting

### Button not responding?
- Check `IsEnabled="True"`
- Verify event handler exists
- Ensure ControlTemplate isn't blocking input

### Text not visible?
- Check contrast: Primary #111827 on white = 12:1 ✓
- Verify Foreground property is set
- Check Background vs Text color

### Shadow not showing?
- Effect must be on Border or container, not TextBlock
- Check opacity > 0
- Verify BlurRadius > 0
- Test on actual machine (designer preview limited)

### Animation too slow?
- Reduce Duration: `0:0:0.15` for fast, `0:0:0.2` for standard
- Check completed event not blocking next animation
- Verify hardware acceleration enabled

---

## Best Practices Checklist

- [ ] Use StaticResource for all colors
- [ ] Apply consistent spacing (16px/24px/40px)
- [ ] Add focus states to all interactive elements
- [ ] Include tooltips on icon-only buttons
- [ ] Test with keyboard navigation (Tab key)
- [ ] Verify color contrast ratios (>4.5:1)
- [ ] Use modern rounded corners (10-20px)
- [ ] Apply soft shadows to cards (StandardShadow)
- [ ] Smooth animations (150-300ms)
- [ ] Disable state for inactive buttons
- [ ] Empty states with helpful messages
- [ ] Loading states with spinner/icon
- [ ] Error states with red text + icon
- [ ] Success states with green checkmark

---

For detailed information, see: **MODERN_SAAS_DESIGN.md**
