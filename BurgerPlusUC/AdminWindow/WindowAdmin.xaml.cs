using BurgerPlusUC.ApplicationDate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BurgerPlusUC.AdminWindow
{
    /// <summary>
    /// Логика взаимодействия для WindowAdmin.xaml
    /// </summary>
    public partial class WindowAdmin : Window
    {
        public WindowAdmin()
        {
            InitializeComponent();
            // Инициализация подключения к БД
            AppConnect.modelBD = DBBurgerPlusEntities1.GetContext();

            // Загрузка данных в таблицу
            LoadUsers();
            // Загрузка ролей в комбобокс
            LoadRoles();
        }

        // Загрузка списка пользователей
        private void LoadUsers()
        {
            dgUsers.ItemsSource = AppConnect.modelBD.user.ToList();
        }

        // Загрузка ролей в выпадающий список
        private void LoadRoles()
        {
            cbRole.ItemsSource = AppConnect.modelBD.role.ToList();
            cbRole.DisplayMemberPath = "role_name";
            cbRole.SelectedValuePath = "role_id";
        }

        // 1. Добавление нового пользователя
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string login = tbLogin.Text.Trim();
            string password = pbPassword.Password;

            // Валидация полей
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || cbRole.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля (Логин, Пароль, Роль)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка уникальности логина
            if (AppConnect.modelBD.user.Any(u => u.user_login == login))
            {
                // Сообщение при попытке добавить существующего пользователя
                MessageBox.Show($"Пользователь с логином '{login}' уже существует!",
                    "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Создание нового пользователя
            user newUser = new user
            {
                user_login = login,
                user_password = password,
                role_id = Convert.ToInt32(cbRole.SelectedValue),
                is_blocked = false,
                failed_attempts = 0
            };

            AppConnect.modelBD.user.Add(newUser);
            MessageBox.Show("Пользователь успешно добавлен", "Успех");
            LoadUsers();

            // Очистка полей
            tbLogin.Clear();
            pbPassword.Clear();
        }

        // 2. Изменения данных текущего пользователя
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя из таблицы для изменения", "Внимание");
                return;
            }

            user selectedUser = dgUsers.SelectedItem as user;
            string newLogin = tbLogin.Text.Trim();
            string newPassword = pbPassword.Password;

            if (string.IsNullOrEmpty(newLogin) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Введите новый логин и пароль в поля выше", "Ошибка");
                return;
            }

            // Если меняем логин, проверяем занятость
            if (selectedUser.user_login != newLogin && AppConnect.modelBD.user.Any(u => u.user_login == newLogin))
            {
                MessageBox.Show($"Логин '{newLogin}' уже занят другим пользователем", "Ошибка");
                return;
            }

            selectedUser.user_login = newLogin;
            selectedUser.user_password = newPassword;
            selectedUser.role_id = Convert.ToInt32(cbRole.SelectedValue);

            AppConnect.modelBD.SaveChanges();
            MessageBox.Show("Данные пользователя обновлены", "Успех");
            LoadUsers();
        }

        // 3. Снятие блокировки
        private void BtnUnblock_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem == null)
            {
                MessageBox.Show("Выберите пользователя для разблокировки", "Внимание");
                return;
            }

            user selectedUser = dgUsers.SelectedItem as user;

            // Снятие блокировки
            if (selectedUser.is_blocked == true)
            {
                selectedUser.is_blocked = false;
                selectedUser.failed_attempts = 0; // Сброс счетчика ошибок
                AppConnect.modelBD.SaveChanges();
                MessageBox.Show($"Пользователь '{selectedUser.user_login}' разблокирован", "Успех");
                LoadUsers();
            }
            else
            {
                MessageBox.Show("Этот пользователь не заблокирован", "Информация");
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
