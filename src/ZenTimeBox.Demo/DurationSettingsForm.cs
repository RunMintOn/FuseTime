using System.Drawing;

namespace ZenTimeBox.Demo;

internal sealed class DurationSettingsForm : Form
{
    private readonly TimerMenuSettingsService settingsService;
    private readonly Action settingsChanged;
    private readonly FlowLayoutPanel listPanel = new();
    private readonly NumericUpDown customInput = new();

    public DurationSettingsForm(TimerMenuSettingsService settingsService, Action settingsChanged)
    {
        this.settingsService = settingsService;
        this.settingsChanged = settingsChanged;

        Text = "ZenTimeBox Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(310, 430);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

        Label title = new()
        {
            Text = "Durations",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(14, 14),
        };

        listPanel.Location = new Point(12, 42);
        listPanel.Size = new Size(286, 300);
        listPanel.FlowDirection = FlowDirection.TopDown;
        listPanel.WrapContents = false;
        listPanel.AutoScroll = true;

        Label customLabel = new()
        {
            Text = "Custom minutes",
            AutoSize = true,
            Location = new Point(14, 358),
        };

        customInput.Minimum = 1;
        customInput.Maximum = 999;
        customInput.Value = 25;
        customInput.Location = new Point(124, 354);
        customInput.Width = 76;

        Button addButton = new()
        {
            Text = "Add",
            Location = new Point(214, 353),
            Width = 72,
        };
        addButton.Click += (_, _) => AddCustomDuration();

        Controls.Add(title);
        Controls.Add(listPanel);
        Controls.Add(customLabel);
        Controls.Add(customInput);
        Controls.Add(addButton);

        RebuildList();
    }

    private void RebuildList()
    {
        listPanel.SuspendLayout();
        listPanel.Controls.Clear();

        TimerMenuSettings settings = settingsService.GetSettings();
        foreach (int minutes in settingsService.GetAllMinutes())
        {
            listPanel.Controls.Add(CreateDurationRow(minutes, settings));
        }

        listPanel.ResumeLayout();
    }

    private Control CreateDurationRow(int minutes, TimerMenuSettings settings)
    {
        Panel row = new()
        {
            Width = 264,
            Height = 32,
            Margin = new Padding(0, 0, 0, 4),
        };

        CheckBox checkBox = new()
        {
            Text = $"{minutes} min",
            Checked = settings.FavoriteMinutes.Contains(minutes),
            AutoSize = true,
            Location = new Point(0, 6),
        };
        checkBox.CheckedChanged += (_, _) => ToggleFavorite(minutes, checkBox.Checked);

        Label kindLabel = new()
        {
            Text = settingsService.IsBuiltIn(minutes) ? "Built-in" : "Custom",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Location = new Point(112, 8),
        };

        row.Controls.Add(checkBox);
        row.Controls.Add(kindLabel);

        if (!settingsService.IsBuiltIn(minutes))
        {
            Button deleteButton = new()
            {
                Text = "Delete",
                Width = 62,
                Height = 25,
                Location = new Point(196, 3),
            };
            deleteButton.Click += (_, _) => DeleteCustomDuration(minutes);
            row.Controls.Add(deleteButton);
        }

        return row;
    }

    private void ToggleFavorite(int minutes, bool enabled)
    {
        TimerMenuSettings settings = settingsService.GetSettings();
        int[] favorites = enabled
            ? settings.FavoriteMinutes.Append(minutes).Distinct().Order().ToArray()
            : settings.FavoriteMinutes.Where(value => value != minutes).ToArray();

        settingsService.SaveSettings(new TimerMenuSettings
        {
            FavoriteMinutes = favorites,
            CustomMinutes = settings.CustomMinutes,
        });
        settingsChanged();
    }

    private void AddCustomDuration()
    {
        int minutes = (int)customInput.Value;
        TimerMenuSettings settings = settingsService.GetSettings();
        settingsService.SaveSettings(new TimerMenuSettings
        {
            FavoriteMinutes = settings.FavoriteMinutes.Append(minutes).Distinct().Order().ToArray(),
            CustomMinutes = settings.CustomMinutes.Append(minutes).Where(value => !settingsService.IsBuiltIn(value)).Distinct().Order().ToArray(),
        });
        settingsChanged();
        RebuildList();
    }

    private void DeleteCustomDuration(int minutes)
    {
        TimerMenuSettings settings = settingsService.GetSettings();
        settingsService.SaveSettings(new TimerMenuSettings
        {
            FavoriteMinutes = settings.FavoriteMinutes.Where(value => value != minutes).ToArray(),
            CustomMinutes = settings.CustomMinutes.Where(value => value != minutes).ToArray(),
        });
        settingsChanged();
        RebuildList();
    }
}
