﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AtlantPrograma
{
    public partial class Form4 : Form
    {
        private string currentUser;
        private string connectionString = "server=localhost;database=document_system;uid=root;pwd=1111;";
        private DataTable originalDataTable; // Полная таблица без фильтра
        private bool isFilterApplied = false;
        public Form4(string username)
        {
            InitializeComponent();
            currentUser = username;
            //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void DisplayUsers(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
            {
                dataGridView1.Columns.Clear();
                dataGridView1.DataSource = null;
                return;
            }

            // Загрузка словаря отделов
            LoadDepartments();

            dataGridView1.Columns.Clear(); // Убираем старые колонки

            dataGridView1.DataSource = table;

            // Добавим чекбокс-колонку
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "",
                Width = 30,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            };
            dataGridView1.Columns.Insert(0, checkColumn);

            // Настройка заголовков на русском
            if (dataGridView1.Columns.Contains("username"))
                dataGridView1.Columns["username"].HeaderText = "Имя пользователя";

            if (dataGridView1.Columns.Contains("phone"))
                dataGridView1.Columns["phone"].HeaderText = "Телефон";

            if (dataGridView1.Columns.Contains("department_name"))
            {
                dataGridView1.Columns["department_name"].HeaderText = "Отдел";

                // Преобразуем в ComboBoxColumn
                DataGridViewComboBoxColumn comboBoxColumn = new DataGridViewComboBoxColumn
                {
                    DataPropertyName = "department_name",
                    HeaderText = "Отдел",
                    Name = "department_name",
                    DataSource = new BindingSource(departmentsDict, null),
                    DisplayMember = "Value",
                    ValueMember = "Value"
                };

                int index = dataGridView1.Columns["department_name"].Index;
                dataGridView1.Columns.Remove("department_name");
                dataGridView1.Columns.Insert(index, comboBoxColumn);
            }

            if (dataGridView1.Columns.Contains("id"))
                dataGridView1.Columns["id"].Visible = false;
        }

        private void LoadUsers()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT u.id, u.username, ud.phone, d.name AS department_name FROM users u " +
                                   "LEFT JOIN user_details ud ON u.id = ud.user_id " +
                                   "LEFT JOIN departments d ON ud.department_id = d.id " +
                                   "WHERE u.username != 'admin'";
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(query, conn);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    originalDataTable = dataTable.Copy(); // Обновляем кэш

                    DisplayUsers(dataTable); // Показываем в таблице
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }     
        private void выйтиИзСистемыToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            bool hasUnsavedChanges = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                int userId = Convert.ToInt32(row.Cells["id"].Value);
                string currentDept = row.Cells["department_name"].Value?.ToString()?.Trim() ?? "";
                string currentPhone = row.Cells["phone"].Value?.ToString()?.Trim() ?? "";

                DataRow[] originalRows = originalDataTable.Select($"id = {userId}");
                if (originalRows.Length == 0) continue;

                string originalDept = originalRows[0]["department_name"]?.ToString()?.Trim() ?? "";
                string originalPhone = originalRows[0]["phone"]?.ToString()?.Trim() ?? "";

                if (currentDept != originalDept || currentPhone != originalPhone)
                {
                    hasUnsavedChanges = true;
                    break; // Нам достаточно одного отличия
                }
            }

            if (hasUnsavedChanges)
            {
                var confirmation = MessageBox.Show("Вы уверены, что хотите выйти без сохранения изменений?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmation != DialogResult.Yes)
                    return;
            }

            // Выход
            this.Hide();
            Form1 loginForm = new Form1();
            loginForm.Show();

            //var confirmation = MessageBox.Show("Вы уверены, что хотите выйти без сохранения изменений?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            //if (confirmation == DialogResult.Yes)
            //{
            //    this.Hide();
            //    Form1 loginForm = new Form1();
            //    loginForm.Show();
            //}
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string input = textBox1.Text.Trim();

            // Проверка на пустую строку поиска
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Пожалуйста, введите имя пользователя для поиска", "Поиск", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Выполнение поиска
            SearchUsers(input, reloadIfEmpty: false); // Поиск без перезагрузки

            // Дополнительная проверка на отсутствие совпадений
            var filteredRows = originalDataTable.AsEnumerable()
                .Where(r => r.Field<string>("username").ToLower().Contains(input.ToLower()))
                .ToList();

            if (filteredRows.Count == 0)
            {
                MessageBox.Show("Пользователь не найден", "Поиск", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selectedUsers = new List<int>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    selectedUsers.Add(Convert.ToInt32(row.Cells["id"].Value));
                }
            }

            int count = selectedUsers.Count;
            if (count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите хотя бы одного пользователя для удаления", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmation = MessageBox.Show(
                $"Вы выбрали {count} пользователь(я)(ей) для удаления.\nПродолжить?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    foreach (var userId in selectedUsers)
                    {
                        string deleteQuery = "DELETE FROM users WHERE id = @userId";
                        MySqlCommand cmd = new MySqlCommand(deleteQuery, conn);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Пользователи удалены успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadUsers();  // Обновляем таблицу
                    SetupSearchAutoComplete();  // Обновляем автозаполнение
                }
            }

            //var selectedUsers = new List<int>();
            //foreach (DataGridViewRow row in dataGridView1.Rows)
            //{
            //    if (Convert.ToBoolean(row.Cells["Select"].Value))
            //    {
            //        selectedUsers.Add(Convert.ToInt32(row.Cells["id"].Value));
            //    }
            //}

            //if (selectedUsers.Count == 0)
            //{
            //    MessageBox.Show("Пожалуйста, выберите хотя бы одного пользователя для удаления", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            //var confirmation = MessageBox.Show("Вы уверены, что хотите удалить выбранных пользователей?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            //if (confirmation == DialogResult.Yes)
            //{
            //    // Удаление пользователей
            //    using (MySqlConnection conn = new MySqlConnection(connectionString))
            //    {
            //        conn.Open();
            //        foreach (var userId in selectedUsers)
            //        {
            //            string deleteQuery = "DELETE FROM users WHERE id = @userId";
            //            MySqlCommand cmd = new MySqlCommand(deleteQuery, conn);
            //            cmd.Parameters.AddWithValue("@userId", userId);
            //            cmd.ExecuteNonQuery();
            //        }

            //        MessageBox.Show("Пользователи удалены успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        LoadUsers();  // Перезагружаем данные
            //                      // Обновляем автозаполнение после удаления пользователей
            //        SetupSearchAutoComplete();
            //    }
            //}
        }

        Dictionary<int, string> departmentsDict = new Dictionary<int, string>();

        private void LoadDepartments()
        {
            departmentsDict.Clear();
            string query = "SELECT id, name FROM departments";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departmentsDict.Add(reader.GetInt32("id"), reader.GetString("name"));
                        }
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool wereAnyChangesMade = false;
            List<string> changedUsernames = new List<string>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                int userId = Convert.ToInt32(row.Cells["id"].Value);
                string newDept = row.Cells["department_name"].Value?.ToString()?.Trim() ?? "";
                string newPhone = row.Cells["phone"].Value?.ToString()?.Trim() ?? "";
                string username = row.Cells["username"].Value?.ToString()?.Trim() ?? ""; // Предполагается, что колонка "username" есть в таблице

                // Ищем строку по ID в оригинальной полной таблице, даже если фильтр активен
                DataRow[] originalRows = originalDataTable.Select($"id = {userId}");
                if (originalRows.Length == 0) continue;

                string originalDept = originalRows[0]["department_name"]?.ToString()?.Trim() ?? "";
                string originalPhone = originalRows[0]["phone"]?.ToString()?.Trim() ?? "";

                if (newDept != originalDept || newPhone != originalPhone)
                {
                    changedUsernames.Add(username);
                    wereAnyChangesMade = true;
                }
            }

            if (!wereAnyChangesMade)
            {
                MessageBox.Show("Вы не внесли никаких изменений", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Формируем сообщение для подтверждения
            string usersList = string.Join(", ", changedUsernames);
            string confirmMessage = $"Вы внесли изменения у {changedUsernames.Count} пользователей:\n{usersList}\n\nПродолжить?";
            var confirmResult = MessageBox.Show(confirmMessage, "Подтверждение изменений", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            // Применяем изменения к базе
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                int userId = Convert.ToInt32(row.Cells["id"].Value);
                string newDept = row.Cells["department_name"].Value?.ToString()?.Trim() ?? "";
                string newPhone = row.Cells["phone"].Value?.ToString()?.Trim() ?? "";

                // Сравним ещё раз, чтобы не писать лишние запросы
                DataRow[] originalRows = originalDataTable.Select($"id = {userId}");
                if (originalRows.Length == 0) continue;

                string originalDept = originalRows[0]["department_name"]?.ToString()?.Trim() ?? "";
                string originalPhone = originalRows[0]["phone"]?.ToString()?.Trim() ?? "";

                if (newDept != originalDept || newPhone != originalPhone)
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string updateQuery = @"
                UPDATE user_details 
                SET department_id = (SELECT id FROM departments WHERE name = @department), 
                    phone = @phone 
                WHERE user_id = @userId";
                        using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@department", newDept);
                            cmd.Parameters.AddWithValue("@phone", newPhone);
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUsers(); // также обновляет originalDataTable

            //foreach (DataGridViewRow row in dataGridView1.Rows)
            //{
            //    if (row.Cells["department_name"].Value != null)
            //    {
            //        string newDepartment = row.Cells["department_name"].Value.ToString();
            //        int userId = Convert.ToInt32(row.Cells["id"].Value);
            //        string phone = row.Cells["phone"].Value?.ToString();

            //        using (MySqlConnection conn = new MySqlConnection(connectionString))
            //        {
            //            conn.Open();
            //            string updateQuery = "UPDATE user_details SET department_id = (SELECT id FROM departments WHERE name = @department), phone = @phone WHERE user_id = @userId";
            //            MySqlCommand cmd = new MySqlCommand(updateQuery, conn);
            //            cmd.Parameters.AddWithValue("@department", newDepartment);
            //            cmd.Parameters.AddWithValue("@phone", phone ?? "");
            //            cmd.Parameters.AddWithValue("@userId", userId);
            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //}

            //MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //LoadUsers(); // Перезагружаем таблицу после сохранения
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Просто подавим ошибку — можно добавить лог или отладочную информацию при необходимости
            e.ThrowException = false;
        }

        Timer autoScrollTimer = new Timer();
        string scrollDirection = null;

        private void AutoScrollTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                int totalWidth = dataGridView1.Columns.Cast<DataGridViewColumn>().Sum(c => c.Width);
                int visibleWidth = dataGridView1.DisplayRectangle.Width;
                int maxScroll = Math.Max(0, totalWidth - visibleWidth);

                if (scrollDirection == "right" && dataGridView1.HorizontalScrollingOffset < maxScroll)
                {
                    dataGridView1.HorizontalScrollingOffset += 20;
                }
                else if (scrollDirection == "left" && dataGridView1.HorizontalScrollingOffset > 0)
                {
                    dataGridView1.HorizontalScrollingOffset = Math.Max(0, dataGridView1.HorizontalScrollingOffset - 20); // фикс
                }
            }
            catch
            {
                autoScrollTimer.Stop();
            }
        }

        private void DataGridView1_MouseLeave(object sender, EventArgs e)
        {
            autoScrollTimer.Stop();
            scrollDirection = null;
        }


        private void DataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            int scrollZoneWidth = 100; // зона в пикселях, где начинается прокрутка

            if (e.X >= dataGridView1.Width - scrollZoneWidth)
            {
                scrollDirection = "right";
                autoScrollTimer.Start();
            }
            else if (e.X <= scrollZoneWidth)
            {
                scrollDirection = "left";
                autoScrollTimer.Start();
            }
            else
            {
                autoScrollTimer.Stop();
                scrollDirection = null;
            }
        }

        private void Form4_Load_1(object sender, EventArgs e)
        {
            autoScrollTimer.Interval = 100; // чем меньше — тем быстрее прокрутка
            autoScrollTimer.Tick += AutoScrollTimer_Tick;
            dataGridView1.MouseMove += DataGridView1_MouseMove;
            dataGridView1.MouseLeave += DataGridView1_MouseLeave;
            LoadUsers();  // Загрузка всех пользователей при открытии формы
            SetupSearchAutoComplete();  // Настройка автозаполнения для поиска
            textBox1.TextChanged += textBox1_TextChanged;
            dataGridView1.EditingControlShowing += dataGridView1_EditingControlShowing;
            dataGridView1.DataError += dataGridView1_DataError;
        }

        private void SetupSearchAutoComplete()
        {
            AutoCompleteStringCollection autoCompleteCollection = new AutoCompleteStringCollection();

            // Получаем актуальный список пользователей из базы данных
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT username FROM users WHERE username != 'admin'"; // Исключаем администратора
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    autoCompleteCollection.Add(reader.GetString("username"));
                }
            }

            // Настроим автозаполнение
            textBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox1.AutoCompleteSource = AutoCompleteSource.CustomSource;
            textBox1.AutoCompleteCustomSource = autoCompleteCollection;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                SearchUsers("", reloadIfEmpty: true); // Только сброс
            }
        }
        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex == dataGridView1.Columns["department_name"].Index && e.Control is ComboBox comboBox)
            {
                comboBox.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            }
        }


        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.SelectedItem is KeyValuePair<int, string> selectedDept)
            {
                string phoneQuery = "SELECT phones FROM departments WHERE id = @id";
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(phoneQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", selectedDept.Key);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            string phones = result.ToString();
                            dataGridView1.CurrentRow.Cells["phone"].Value = phones; // ⚠ Используй точное имя столбца
                        }
                        else
                        {
                            dataGridView1.CurrentRow.Cells["phone"].Value = "";
                        }
                    }
                }
            }
        }

        // Вспомогательный метод — диалоговое окно для выбора нескольких отделов
        private List<string> ShowSelectDialogForMultipleDepartments(string title, List<string> items)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 280,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label textLabel = new Label() { Left = 50, Top = 10, Text = "Выберите отделы:", AutoSize = true };
            CheckedListBox checkedListBox = new CheckedListBox() { Left = 50, Top = 35, Width = 200, Height = 150 };
            checkedListBox.Items.AddRange(items.ToArray());

            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 100, Top = 200, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(checkedListBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                // Получаем выбранные отделы
                var selectedDepartments = checkedListBox.CheckedItems.Cast<string>().ToList();
                return selectedDepartments;
            }
            else
            {
                return null;
            }
        }

        private DataTable currentFilteredData = null;
        private void поОтделамToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> departments = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT name FROM departments ORDER BY name";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        departments.Add(reader.GetString("name"));
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении списка отделов: " + ex.Message);
                    return;
                }
            }

            // Цикл для повторной попытки выбора, пока фильтр не применён или пользователь не отменит
            while (true)
            {
                var selectedDepts = ShowSelectDialogForMultipleDepartments("Выберите отделы", departments);

                if (selectedDepts == null || !selectedDepts.Any())
                    return;

                // Фильтруем на основе выбранных отделов
                var filteredRows = originalDataTable.AsEnumerable()
                    .Where(r => selectedDepts.Contains(r.Field<string>("department_name")));

                if (!filteredRows.Any())
                {
                    MessageBox.Show("Пользователей из выбранных отделов не найдено", "Фильтрация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // Сбрасываем выбор и продолжаем фильтрацию
                    continue;
                }

                // Проверим, какие из выбранных отделов есть в данных
                var availableDepartments = selectedDepts.Where(dept => originalDataTable.AsEnumerable()
                    .Any(r => r.Field<string>("department_name") == dept)).ToList();

                if (!availableDepartments.Any())
                {
                    MessageBox.Show("Нет доступных отделов из выбранных", "Фильтрация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                // Фильтруем только по тем отделам, которые есть в данных
                filteredRows = originalDataTable.AsEnumerable()
                    .Where(r => availableDepartments.Contains(r.Field<string>("department_name")));

                var filtered = filteredRows.CopyToDataTable();

                // Сохраняем данные после фильтра по отделам
                currentFilteredData = filtered;

                // Применяем отображение
                DisplayUsers(filtered);
                isFilterApplied = true; // Устанавливаем флаг, что фильтр применён
                break; // Успешно применили фильтр — выходим
            }
        }

        private void поВозрастаниюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Если фильтрация по отделам не была применена, используем исходные данные для сортировки
            DataTable dataToSort = currentFilteredData ?? originalDataTable;

            var sorted = dataToSort.AsEnumerable()
                .OrderBy(r => r.Field<string>("username"), StringComparer.Create(new System.Globalization.CultureInfo("ru-RU"), ignoreCase: true))
                .CopyToDataTable();

            DisplayUsers(sorted); // Показываем отсортированные данные
            isFilterApplied = true; // Устанавливаем флаг фильтрации
        }

        private void поУбываниюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Если фильтрация по отделам не была применена, используем исходные данные для сортировки
            DataTable dataToSort = currentFilteredData ?? originalDataTable;

            var sorted = dataToSort.AsEnumerable()
                .OrderByDescending(r => r.Field<string>("username"), StringComparer.Create(new System.Globalization.CultureInfo("ru-RU"), ignoreCase: true))
                .CopyToDataTable();

            DisplayUsers(sorted); // Показываем отсортированные данные
            isFilterApplied = true; // Устанавливаем флаг фильтрации
        }

        private void сброситьФильтрыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isFilterApplied)
            {
                MessageBox.Show("Фильтры ещё не были применены. Сброс невозможен!", "Сброс фильтров", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show("Вы действительно хотите сбросить фильтры и отобразить всех пользователей?", "Сброс фильтров", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                currentFilteredData = null; // Сбрасываем данные фильтрации
                DisplayUsers(originalDataTable); // Отображаем все данные
                isFilterApplied = false; // Сброс флага фильтра
            }
        }

        private void SearchUsers(string searchText, bool reloadIfEmpty = true)
        {
            // Сброс старого выделения
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Style.BackColor = Color.White; // Исходный цвет
                }
            }

            // Если строка поиска пуста
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Только сбросить подсветку
                return;
            }

            bool firstMatchFound = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["username"].Value != null)
                {
                    string username = row.Cells["username"].Value.ToString();
                    if (username.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        row.Cells["username"].Style.BackColor = Color.Yellow;

                        if (!firstMatchFound)
                        {
                            dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                            firstMatchFound = true;
                        }
                    }
                }
            }
        }

        private void EditSelectedUsers()
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            List<int> selectedUserIds = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var cellValue = row.Cells["Select"].Value;
                bool isSelected = cellValue != null && cellValue is bool && (bool)cellValue;

                if (isSelected)
                {
                    int userId = Convert.ToInt32(row.Cells["id"].Value);
                    selectedUserIds.Add(userId);
                }
            }

            int selectedCount = selectedUserIds.Count;

            if (selectedCount == 0)
            {
                MessageBox.Show("Не выбраны пользователи", "Изменение данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Вы выбрали {selectedCount} пользователь(я)(ей). Продолжить?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            int editedCount = 0;

            foreach (int userId in selectedUserIds)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT u.username, u.password, ud.department_id, d.phones AS phone
                FROM users u
                LEFT JOIN user_details ud ON ud.user_id = u.id
                LEFT JOIN departments d ON d.id = ud.department_id
                WHERE u.id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);

                        string username = "";
                        string password = "";
                        string departmentName = "";
                        string phone = "";

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                continue;

                            username = reader["username"].ToString();
                            password = "";

                            int departmentId = reader["department_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["department_id"]);
                            departmentName = departmentId != 0 && departmentsDict.ContainsKey(departmentId)
                                ? departmentsDict[departmentId]
                                : "";

                            phone = reader["phone"] == DBNull.Value ? "" : reader["phone"].ToString();
                        }

                        if (username == currentUser)
                        {
                            MessageBox.Show("Нельзя изменить данные текущего пользователя", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            continue;
                        }

                        while (true)
                        {
                            try
                            {
                                var result = ShowEditDialog(userId, username, password, departmentName, phone);

                                if (result == DialogResult.Cancel)
                                    break; // пользователь нажал Отмена — перейти к следующему

                                if (result == DialogResult.OK)
                                {
                                    editedCount++;
                                    break; // изменения успешно применены — перейти к следующему
                                }

                                // DialogResult.Retry — остаёмся в этом же цикле
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ошибка при редактировании пользователя с ID {userId}:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break; // при исключении — выйти из цикла
                            }
                        }
                    }
                }
            }

            if (editedCount == 0)
            {
                //MessageBox.Show("Изменения не были внесены", "Изменение данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Данные успешно обновлены для {editedCount} пользователь(ей)!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            LoadUsers(); // Обновить таблицу
        }

        private DialogResult ShowEditDialog(int userId, string username, string passwordHash, string departmentName, string phone)
        {
            Form dialog = new Form()
            {
                Width = 500,
                Height = 320,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = $"Изменение: {username}",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblUsername = new Label() { Text = "Имя пользователя:", Top = 20, Left = 10, Width = 140 };
            TextBox txtUsername = new TextBox() { Top = 20, Left = 160, Width = 300, Text = username };

            Label lblPassword = new Label() { Text = "Пароль (необязательно):", Top = 60, Left = 10, Width = 140 };
            TextBox txtPassword = new TextBox() { Top = 60, Left = 160, Width = 260, UseSystemPasswordChar = true };

            PictureBox pictureBox1 = new PictureBox()
            {
                Left = 430,
                Top = 60,
                Width = 24,
                Height = 24,
                Cursor = Cursors.Hand,
                SizeMode = PictureBoxSizeMode.StretchImage,
                ImageLocation = "D:\\diplom\\eye.ico"
            };

            bool passwordVisible = false;
            pictureBox1.Click += (s, e) =>
            {
                passwordVisible = !passwordVisible;
                txtPassword.UseSystemPasswordChar = !passwordVisible;
            };

            Label lblDepartment = new Label() { Text = "Отдел:", Top = 100, Left = 10, Width = 140 };
            ComboBox cbDepartment = new ComboBox()
            {
                Top = 100,
                Left = 160,
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (var dep in departmentsDict)
                cbDepartment.Items.Add(dep.Value);

            cbDepartment.SelectedItem = departmentName;

            Label lblPhone = new Label() { Text = "Телефон отдела:", Top = 140, Left = 10, Width = 140 };
            TextBox txtPhone = new TextBox() { Top = 140, Left = 160, Width = 300, Text = phone ?? "" };

            Button btnSave = new Button() { Text = "Сохранить", Left = 280, Width = 90, Top = 220, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Отмена", Left = 380, Width = 90, Top = 220, DialogResult = DialogResult.Cancel };

            dialog.Controls.AddRange(new Control[] {
        lblUsername, txtUsername,
        lblPassword, txtPassword, pictureBox1,
        lblDepartment, cbDepartment,
        lblPhone, txtPhone,
        btnSave, btnCancel
    });

            dialog.AcceptButton = btnSave;
            dialog.CancelButton = btnCancel;

            cbDepartment.SelectedIndexChanged += (s, e) =>
            {
                string selectedDep = cbDepartment.SelectedItem?.ToString();
                txtPhone.Text = GetPhonesForDepartment(selectedDep) ?? "";
            };

            while (true)
            {
                var result = dialog.ShowDialog();

                if (result == DialogResult.Cancel)
                    return DialogResult.Cancel;

                string newUsername = txtUsername.Text;
                string newPassword = txtPassword.Text;
                string newDepartmentName = cbDepartment.SelectedItem?.ToString() ?? "";
                string newPhone = txtPhone.Text.Trim();

                bool hasChanges = newUsername != username
                                  || !string.IsNullOrEmpty(newPassword)
                                  || newDepartmentName != departmentName
                                  || newPhone != (phone ?? "");

                if (!hasChanges)
                {
                    MessageBox.Show("Вы не внесли никаких изменений", "Нет изменений", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                // Если номер телефона не пустой — проверим формат
                if (!string.IsNullOrEmpty(newPhone) &&
                    !Regex.IsMatch(newPhone, @"^\d-\d{2}(, \d-\d{2})*$"))
                {
                    MessageBox.Show("Номер телефона должен быть в формате 4-20 или 4-20, 9-10", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                // Обновление данных
                int departmentId = departmentsDict.FirstOrDefault(x => x.Value == newDepartmentName).Key;
                UpdateUser(userId, newUsername, newPassword, departmentId, newPhone);
                return DialogResult.OK;
            }
        }


        private void UpdateUser(int userId, string newUsername, string password, int departmentId, string phones)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Получить старое имя пользователя
                string oldUsername = "";
                using (MySqlCommand getOldUsernameCmd = new MySqlCommand(
                    "SELECT username FROM users WHERE id = @id", conn))
                {
                    getOldUsernameCmd.Parameters.AddWithValue("@id", userId);
                    using (var reader = getOldUsernameCmd.ExecuteReader())
                    {
                        if (reader.Read())
                            oldUsername = reader.GetString(0);
                    }
                }

                // Обновить имя и (возможно) пароль пользователя
                MySqlCommand cmdUser;
                if (!string.IsNullOrEmpty(password))
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                    cmdUser = new MySqlCommand(
                        "UPDATE users SET username = @username, password = @password WHERE id = @id", conn);
                    cmdUser.Parameters.AddWithValue("@password", hashedPassword);
                }
                else
                {
                    cmdUser = new MySqlCommand(
                        "UPDATE users SET username = @username WHERE id = @id", conn);
                }

                cmdUser.Parameters.AddWithValue("@username", newUsername);
                cmdUser.Parameters.AddWithValue("@id", userId);
                cmdUser.ExecuteNonQuery();

                // Обновить отдел в user_details
                MySqlCommand cmdDetails = new MySqlCommand(
                    "UPDATE user_details SET department_id = @depId WHERE user_id = @id", conn);
                cmdDetails.Parameters.AddWithValue("@depId", departmentId);
                cmdDetails.Parameters.AddWithValue("@id", userId);
                cmdDetails.ExecuteNonQuery();

                // Обновить номер телефона отдела в departments
                MySqlCommand cmdDeptPhone = new MySqlCommand(
                    "UPDATE departments SET phones = @phones WHERE id = @id", conn);
                cmdDeptPhone.Parameters.AddWithValue("@phones", phones);
                cmdDeptPhone.Parameters.AddWithValue("@id", departmentId);
                cmdDeptPhone.ExecuteNonQuery();

                // Обновить номер телефона у всех пользователей этого отдела
                MySqlCommand updatePhoneInDetails = new MySqlCommand(
                    "UPDATE user_details SET phone = @phones WHERE department_id = @depId", conn);
                updatePhoneInDetails.Parameters.AddWithValue("@phones", phones);
                updatePhoneInDetails.Parameters.AddWithValue("@depId", departmentId);
                updatePhoneInDetails.ExecuteNonQuery();

                // Обновить имя пользователя в messages и drafts (sender и recipient)
                if (!string.IsNullOrEmpty(oldUsername) && oldUsername != newUsername)
                {
                    // messages: sender
                    MySqlCommand updateMessagesSender = new MySqlCommand(
                        "UPDATE messages SET sender = @newUsername WHERE sender = @oldUsername", conn);
                    updateMessagesSender.Parameters.AddWithValue("@newUsername", newUsername);
                    updateMessagesSender.Parameters.AddWithValue("@oldUsername", oldUsername);
                    updateMessagesSender.ExecuteNonQuery();

                    // messages: recipient
                    MySqlCommand updateMessagesRecipient = new MySqlCommand(
                        "UPDATE messages SET recipient = @newUsername WHERE recipient = @oldUsername", conn);
                    updateMessagesRecipient.Parameters.AddWithValue("@newUsername", newUsername);
                    updateMessagesRecipient.Parameters.AddWithValue("@oldUsername", oldUsername);
                    updateMessagesRecipient.ExecuteNonQuery();

                    // drafts: sender
                    MySqlCommand updateDraftsSender = new MySqlCommand(
                        "UPDATE drafts SET sender = @newUsername WHERE sender = @oldUsername", conn);
                    updateDraftsSender.Parameters.AddWithValue("@newUsername", newUsername);
                    updateDraftsSender.Parameters.AddWithValue("@oldUsername", oldUsername);
                    updateDraftsSender.ExecuteNonQuery();

                    // drafts: recipient
                    MySqlCommand updateDraftsRecipient = new MySqlCommand(
                        "UPDATE drafts SET recipient = @newUsername WHERE recipient = @oldUsername", conn);
                    updateDraftsRecipient.Parameters.AddWithValue("@newUsername", newUsername);
                    updateDraftsRecipient.Parameters.AddWithValue("@oldUsername", oldUsername);
                    updateDraftsRecipient.ExecuteNonQuery();
                }
            }
        }


        private string GetPhonesForDepartment(string departmentName)
        {
            int depId = departmentsDict.FirstOrDefault(x => x.Value == departmentName).Key;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT phones FROM departments WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", depId);
                return cmd.ExecuteScalar()?.ToString();
            }
        }

        private void изменитьДанныеПользователейToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            EditSelectedUsers();
        }
    }
}
