using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using ShopeeVideoUploader.Models;
using ShopeeVideoUploader.Services;

namespace ShopeeVideoUploader.Controls;

public sealed class FolderManagerDialog : Form
{
    private readonly DatabaseService _dbService;
    private readonly Guna2DataGridView _grid;
    private readonly Guna2TextBox _txtName;
    private readonly Guna2Button _btnAdd;
    private readonly Guna2Button _btnUpdate;
    private readonly Guna2Button _btnDelete;
    private List<FolderItem> _folders = [];

    public FolderManagerDialog(DatabaseService dbService)
    {
        _dbService = dbService;
        
        Text = "Quản lý Chiến dịch / Thư mục";
        Size = new Size(400, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var panelTop = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
        _txtName = new Guna2TextBox { Width = 200, Height = 36, PlaceholderText = "Tên chiến dịch mới", Left = 10 };
        _btnAdd = new Guna2Button { Text = "Thêm", Width = 80, Height = 36, Left = 220, Cursor = Cursors.Hand, FillColor = Color.FromArgb(96, 82, 218), BorderRadius = 4 };
        panelTop.Controls.Add(_txtName);
        panelTop.Controls.Add(_btnAdd);

        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };
        _btnUpdate = new Guna2Button { Text = "Cập nhật", Width = 90, Height = 36, Left = 10, Cursor = Cursors.Hand, FillColor = Color.FromArgb(40, 167, 69), BorderRadius = 4, Enabled = false };
        _btnDelete = new Guna2Button { Text = "Xóa", Width = 90, Height = 36, Left = 110, Cursor = Cursors.Hand, FillColor = Color.FromArgb(220, 53, 69), BorderRadius = 4, Enabled = false };
        panelBottom.Controls.Add(_btnUpdate);
        panelBottom.Controls.Add(_btnDelete);

        _grid = new Guna2DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            GridColor = Color.FromArgb(231, 229, 255)
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Tên chiến dịch", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        Controls.Add(_grid);
        Controls.Add(panelTop);
        Controls.Add(panelBottom);

        _btnAdd.Click += BtnAdd_Click;
        _btnUpdate.Click += BtnUpdate_Click;
        _btnDelete.Click += BtnDelete_Click;
        _grid.SelectionChanged += Grid_SelectionChanged;

        LoadData();
    }

    private void LoadData()
    {
        _folders = _dbService.GetAllFolders();
        _grid.Rows.Clear();
        foreach (var folder in _folders)
        {
            _grid.Rows.Add(folder.Id, folder.Name);
        }
        _grid.ClearSelection();
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        var hasSelection = _grid.SelectedRows.Count > 0;
        _btnUpdate.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
        if (hasSelection)
        {
            _txtName.Text = _grid.SelectedRows[0].Cells["Name"].Value?.ToString() ?? "";
        }
        else
        {
            _txtName.Text = "";
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        
        var folder = new FolderItem { Name = name };
        _dbService.SaveFolder(folder);
        LoadData();
        _txtName.Text = "";
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;
        
        var id = Convert.ToInt32(_grid.SelectedRows[0].Cells["Id"].Value);
        var newName = _grid.SelectedRows[0].Cells["Name"].Value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(newName)) return;
        
        var folder = new FolderItem { Id = id, Name = newName };
        _dbService.UpdateFolder(folder);
        LoadData();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) return;
        
        var id = Convert.ToInt32(_grid.SelectedRows[0].Cells["Id"].Value);
        if (MessageBox.Show("Bạn có chắc muốn xóa chiến dịch này? Các công việc thuộc chiến dịch sẽ bị chuyển về Chưa phân loại.", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            _dbService.DeleteFolder(id);
            LoadData();
        }
    }
}
