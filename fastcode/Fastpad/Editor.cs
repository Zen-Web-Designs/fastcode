using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Fastpad
{
    public partial class Editor : Form
    {
        public static readonly Font EditorFont = new Font("Consolas", 14);

        public Theme Theme { get; private set; }
        
        bool _supresstab;
        int _textlen;
        Keys prevKey;
        RunProgram runProgram;
        AutoCompleter autoCompleter;

        FileInfo currentFileInfo;

        public Editor(Theme theme)
        {
            InitializeComponent();
            this.Theme = theme;
            autoCompleter = new AutoCompleter();
            runProgram = new RunProgram();
            suggestionsBox.Font = EditorFont;
            textBox.Font = EditorFont; 
            textBox.BackColor = Theme.BackColor;
            textBox.ForeColor = Theme.ForeColor;
            _supresstab = false;
        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
            if(_supresstab)
            {
                if(textBox.SelectionLength == 0 && textBox.Text[textBox.SelectionStart-1] == '\t')
                {
                    int oldpos = textBox.SelectionStart;
                    textBox.Text = textBox.Text.Remove(textBox.SelectionStart - 1, 1);
                    _supresstab = false;
                    textBox.SelectionStart = oldpos - 1;
                    return;
                }
            }

            if (SyntaxHighlighter.canTokenize(textBox.Text))
            {
                if (Math.Abs(textBox.TextLength - _textlen) > 1)
                {
                    SyntaxHighlighter.HighlightAll(textBox, Theme,0,textBox.TextLength);
                }
                else
                {
                    if (textBox.TextLength > 0 && textBox.GetLineFromCharIndex(textBox.SelectionStart) < textBox.Lines.Length && textBox.Lines[textBox.GetLineFromCharIndex(textBox.SelectionStart)].Length != 0) 
                    { 
                        SyntaxHighlighter.HighlightAll(textBox, Theme, textBox.GetFirstCharIndexOfCurrentLine(), textBox.Lines[textBox.GetLineFromCharIndex(textBox.SelectionStart)].Length);
                    }
                }
                autoCompleter.ScanForIdentifiers(textBox);
            }

            if (textBox.SelectionLength == 0) 
            { 
                string selected_text = AutoCompleter.SelectCurrentKeyword(textBox);

                if (autoCompleter.IsKeyword(selected_text) || string.IsNullOrWhiteSpace(selected_text))
                {
                    suggestionsBox.Hide();
                }
                else
                {
                    suggestionsBox.Show();
                    Point newPt = textBox.GetPositionFromCharIndex(textBox.SelectionStart - 1);
                    suggestionsBox.Location = new Point(newPt.X, newPt.Y + 25);
                    suggestionsBox.Items.Clear();
                    suggestionsBox.Items.AddRange(autoCompleter.SearchForSuggestions(selected_text));
                    suggestionsBox.SelectedIndex = 0;
                }
            }
            _textlen = textBox.TextLength;
        }

        private void textBox_Click(object sender, EventArgs e)
        {
            suggestionsBox.Hide();
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (suggestionsBox.Visible && e.KeyCode == Keys.Up)
            {
                if (suggestionsBox.SelectedIndex > 0)
                {
                    suggestionsBox.SelectedIndex--;
                }
            }
            else if(suggestionsBox.Visible && e.KeyCode == Keys.Down)
            {
                if(suggestionsBox.SelectedIndex < suggestionsBox.Items.Count-1)
                {
                    suggestionsBox.SelectedIndex++;
                }
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                suggestionsBox.Hide();
            }
            else if(e.KeyCode == Keys.Tab && prevKey != Keys.Tab && suggestionsBox.Visible)
            {
                AutoCompleter.CompleteCurrentKeyword(textBox, (string)suggestionsBox.SelectedItem);
                _supresstab = true;
            }
            else if(e.KeyCode == Keys.F5)
            {
                startToolStripMenuItem_Click(sender, e);
            }
            else if(e.KeyCode == Keys.S && e.Control)
            {
                if(currentFileInfo == null)
                {
                    saveAsToolStripMenuItem_Click(sender, e);
                }
                else
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
            }
            prevKey = e.KeyCode;
        }

        private void mouseMove(object sender, MouseEventArgs e)
        {
            if(e.Y < toolStrip.Height)
            {
                toolStrip.Show();
            }
            else
            {
                toolStrip.Hide();
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    currentFileInfo = new FileInfo(dialog.FileName);
                    textBox.Text = File.ReadAllText(currentFileInfo.FullName);
                    saveToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            File.WriteAllText(currentFileInfo.FullName, textBox.Text);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(SaveFileDialog dialog = new SaveFileDialog())
            {
                if(dialog.ShowDialog() == DialogResult.OK)
                {
                    currentFileInfo = new FileInfo(dialog.FileName);
                    saveToolStripMenuItem_Click(sender, e);
                    saveToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentFileInfo == null)
            {
                runProgram.Run(textBox.Text, Environment.CurrentDirectory);
            }
            else
            {
                runProgram.Run(textBox.Text, currentFileInfo.DirectoryName);
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            runProgram.Stop();
        }
    }
}
