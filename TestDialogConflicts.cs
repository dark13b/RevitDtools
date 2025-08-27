using WinFormsOpenFileDialog = System.Windows.Forms.OpenFileDialog;
using WinFormsSaveFileDialog = System.Windows.Forms.SaveFileDialog;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows;

namespace RevitDtools.Test
{
    /// <summary>
    /// Test file to demonstrate dialog conflicts that need resolution
    /// </summary>
    public class TestDialogConflicts
    {
        public void TestWinFormsDialogs()
        {
            // These should be resolved to WinForms aliases
            var openDialog = new WinFormsOpenFileDialog();
            var saveDialog = new WinFormsSaveFileDialog();
            var folderDialog = new WinFormsFolderBrowserDialog();
            
            if (openDialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                var fileName = openDialog.FileName;
            }
            
            if (saveDialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                var fileName = saveDialog.FileName;
            }
            
            if (folderDialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                var path = folderDialog.SelectedPath;
            }
        }
        
        public void TestWpfDialogs()
        {
            // These should be resolved to WPF aliases
            var wpfOpen = new WpfOpenFileDialog();
            var wpfSave = new WpfSaveFileDialog();
            
            if (wpfOpen.ShowDialog() == true)
            {
                var fileName = wpfOpen.FileName;
            }
            
            if (wpfSave.ShowDialog() == true)
            {
                var fileName = wpfSave.FileName;
            }
        }
    }
}