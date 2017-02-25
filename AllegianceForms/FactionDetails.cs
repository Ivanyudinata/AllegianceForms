﻿using AllegianceForms.Engine;
using AllegianceForms.Engine.Factions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AllegianceForms
{
    public partial class FactionDetails : Form
    {
        public Faction Faction { get; set; }

        public const string PresetFolder = ".\\Data\\FactionPresets";

        public FactionDetails()
        {
            InitializeComponent();

            if (!Directory.Exists(PresetFolder)) return;

            var presetFiles = Directory.GetFiles(PresetFolder);
            var filenames = (from f in presetFiles
                             select f.Substring(f.LastIndexOf("\\") + 1)).ToArray();

            CustomPresets.Items.AddRange(filenames);
        }

        public void LoadFaction(Faction f)
        {
            Faction = f;

            FactionName.Text = f.Name;
            ResearchTime.Text = (1 - f.Bonuses.ResearchTime).ToString("P0");
            ResearchCost.Text = (1 - Faction.Bonuses.ResearchCost).ToString("P0");
            Speed.Text = f.Bonuses.Speed.ToString("P0");
            Health.Text = f.Bonuses.Health.ToString("P0");
            ScanRange.Text = f.Bonuses.ScanRange.ToString("P0");
            Signature.Text = f.Bonuses.Signature.ToString("P0");
            FireRate.Text = f.Bonuses.FireRate.ToString("P0");
            MissileSpeed.Text = f.Bonuses.MissileSpeed.ToString("P0");
            MissileTracking.Text = f.Bonuses.MissileTracking.ToString("P0");
            MiningEfficiency.Text = f.Bonuses.MiningEfficiency.ToString("P0");
            MiningCapacity.Text = f.Bonuses.MiningCapacity.ToString("P0");

            RefreshBalance();
        }

        private void Random_Click(object sender, EventArgs e)
        {
            SoundEffect.Play(ESounds.mousedown);
            CustomPresets.Text = string.Empty;
            LoadFaction(Faction.Random());
        }

        private void FactionDetails_Load(object sender, EventArgs e)
        {
            SoundEffect.Play(ESounds.windowslides);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(PresetFolder)) Directory.CreateDirectory(PresetFolder);

            var filename = PresetFolder + "\\" + CustomPresets.Text;
            if (CustomPresets.Text == string.Empty || File.Exists(filename))
            {
                SoundEffect.Play(ESounds.outofammo);
                return;
            }

            SoundEffect.Play(ESounds.mousedown);
            Utils.SerialiseToFile(filename, Faction);
            if (!CustomPresets.Items.Contains(CustomPresets.Text)) CustomPresets.Items.Add(CustomPresets.Text);
        }

        private void Load_Click(object sender, EventArgs e)
        {
            var filename = PresetFolder + "\\" + CustomPresets.Text;
            if (CustomPresets.Text == string.Empty || !File.Exists(filename))
            {
                SoundEffect.Play(ESounds.outofammo);
                return;
            }

            SoundEffect.Play(ESounds.mousedown);
            var f = Utils.DeserialiseFromFile<Faction>(filename);
            if (f == null) return;
            LoadFaction(f);
        }

        private void RandomName_Click(object sender, EventArgs e)
        {
            SoundEffect.Play(ESounds.mousedown);
            FactionName.Text = Faction.Name = Faction.FactionNames.NextString;
        }

        private void RefreshBalance()
        {
            var f = Faction;

            SoundEffect.Play(ESounds.mousedown);
            var balance = Math.Round(f.Bonuses.TotalBonus,2);

            Speed.ForeColor = GetColour(f.Bonuses.Speed);
            ResearchTime.ForeColor = GetColour(f.Bonuses.ResearchTime);
            ResearchCost.ForeColor = GetColour(f.Bonuses.ResearchCost);
            Health.ForeColor = GetColour(f.Bonuses.Health);
            ScanRange.ForeColor = GetColour(f.Bonuses.ScanRange);
            Signature.ForeColor = GetColour(f.Bonuses.Signature);
            FireRate.ForeColor = GetColour(f.Bonuses.FireRate);
            MissileSpeed.ForeColor = GetColour(f.Bonuses.MissileSpeed);
            MissileTracking.ForeColor = GetColour(f.Bonuses.MissileTracking);
            MiningEfficiency.ForeColor = GetColour(f.Bonuses.MiningEfficiency);
            MiningCapacity.ForeColor = GetColour(f.Bonuses.MiningCapacity);

            BalancedLabel.Text = balance == FactionBonus.NumBonuses ? "Balanced" : (balance < 11 ? "Too Low" : "Too High");
            BalancedLabel.ForeColor = balance == FactionBonus.NumBonuses ? Color.Lime : Color.Red;
            Done.Enabled = balance == FactionBonus.NumBonuses;
        }

        private Color GetColour(float value)
        {
            var r = Math.Round(value, 2);
            return (r == 1) ? Color.DarkGreen : (r > 1) ? Color.Lime : Color.DarkRed;
        }

        private void ResearchTimeUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ResearchTime += 0.1f;
            ResearchTime.Text = (1 - Faction.Bonuses.ResearchTime).ToString("P0");
            RefreshBalance();
        }

        private void ResearchTimeDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ResearchTime -= 0.1f;
            ResearchTime.Text = (1 - Faction.Bonuses.ResearchTime).ToString("P0");
            RefreshBalance();
        }

        private void ResearchCostUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ResearchCost += 0.1f;
            ResearchCost.Text = (1 - Faction.Bonuses.ResearchCost).ToString("P0");
            RefreshBalance();
        }

        private void ResearchCostDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ResearchCost -= 0.1f;
            ResearchCost.Text = (1 - Faction.Bonuses.ResearchCost).ToString("P0");
            RefreshBalance();
        }

        private void SpeedUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Speed += 0.1f;
            Speed.Text = Faction.Bonuses.Speed.ToString("P0");
            RefreshBalance();
        }

        private void SpeedDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Speed -= 0.1f;
            Speed.Text = Faction.Bonuses.Speed.ToString("P0");
            RefreshBalance();
        }

        private void HealthUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Health += 0.1f;
            Health.Text = Faction.Bonuses.Health.ToString("P0");
            RefreshBalance();
        }

        private void ScanRangeUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ScanRange += 0.1f;
            ScanRange.Text = Faction.Bonuses.ScanRange.ToString("P0");
            RefreshBalance();
        }

        private void SignatureUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Signature += 0.1f;
            Signature.Text = Faction.Bonuses.Signature.ToString("P0");
            RefreshBalance();
        }

        private void FireRateUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.FireRate += 0.1f;
            FireRate.Text = Faction.Bonuses.FireRate.ToString("P0");
            RefreshBalance();
        }

        private void MiningCapacityUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MiningCapacity += 0.1f;
            MiningCapacity.Text = Faction.Bonuses.MiningCapacity.ToString("P0");
            RefreshBalance();
        }

        private void MiningCapacityDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MiningCapacity -= 0.1f;
            MiningCapacity.Text = Faction.Bonuses.MiningCapacity.ToString("P0");
            RefreshBalance();
        }

        private void FireRateDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.FireRate -= 0.1f;
            FireRate.Text = Faction.Bonuses.FireRate.ToString("P0");
            RefreshBalance();
        }

        private void SignatureDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Signature -= 0.1f;
            Signature.Text = Faction.Bonuses.Signature.ToString("P0");
            RefreshBalance();
        }

        private void ScanRangeDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.ScanRange -= 0.1f;
            ScanRange.Text = Faction.Bonuses.ScanRange.ToString("P0");
            RefreshBalance();
        }

        private void HealthDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.Health -= 0.1f;
            Health.Text = Faction.Bonuses.Health.ToString("P0");
            RefreshBalance();
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            SoundEffect.Play(ESounds.mouseover);
            var b = sender as Button;
            if (b != null) b.BackColor = Color.DarkGreen;
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            var b = sender as Button;
            if (b != null) b.BackColor = Color.Black;
        }

        private void MissileSpeedUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MissileSpeed += 0.1f;
            MissileSpeed.Text = Faction.Bonuses.MissileSpeed.ToString("P0");
            RefreshBalance();
        }

        private void MissileSpeedDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MissileSpeed -= 0.1f;
            MissileSpeed.Text = Faction.Bonuses.MissileSpeed.ToString("P0");
            RefreshBalance();
        }

        private void MissileTrackingUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MissileTracking += 0.1f;
            MissileTracking.Text = Faction.Bonuses.MissileTracking.ToString("P0");
            RefreshBalance();
        }

        private void MissileTrackingDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MissileTracking -= 0.1f;
            MissileTracking.Text = Faction.Bonuses.MissileTracking.ToString("P0");
            RefreshBalance();
        }

        private void MiningSpeedUp_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MiningEfficiency += 0.1f;
            MiningEfficiency.Text = Faction.Bonuses.MiningEfficiency.ToString("P0");
            RefreshBalance();
        }

        private void MiningSpeedDown_Click(object sender, EventArgs e)
        {
            Faction.Bonuses.MiningEfficiency -= 0.1f;
            MiningEfficiency.Text = Faction.Bonuses.MiningEfficiency.ToString("P0");
            RefreshBalance();
        }
    }
}
