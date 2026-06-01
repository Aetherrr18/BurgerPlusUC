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

namespace BurgerPlusUC.AuthorizationWindow
{
    /// <summary>
    /// Логика взаимодействия для WindowAuthorization.xaml
    /// </summary>
    public partial class WindowAuthorization : Window
    {
        private string _captchaCode;
        private int _captchaFailedAttempts = 0;
        private const int MaxAttempts = 3;
        private string _tempLogin;
        private string _tempPassword;
        private user _currentUser;

        public WindowAuthorization()
        {
            InitializeComponent();
            AppConnect.modelBD = DBBurgerPlusEntities1.GetContext();
        }

        private void AuthorizationButton_Click(object sender, RoutedEventArgs e)
        {
            string loginUser = loginBox.Text.Trim();
            string passwordUser = passwordBox.Password;

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(loginUser) || string.IsNullOrEmpty(passwordUser))
            {
                MessageBox.Show("Введите логин и пароль!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка. Существует ли пользователь с такими данными
            _currentUser = AppConnect.modelBD.user
                .FirstOrDefault(u => u.user_login == loginUser && u.user_password == passwordUser);

            // Обработка неверного логина и пароля
            if (_currentUser == null)
            {
                var userToBlock = AppConnect.modelBD.user
                    .FirstOrDefault(u => u.user_login == loginUser);

                if (userToBlock != null)
                {
                    // Увеличиваем счетчик неудачных попыток
                    userToBlock.failed_attempts = userToBlock.failed_attempts + 1;

                    // Блокировка после 3 попытки
                    if (userToBlock.failed_attempts >= MaxAttempts)
                    {
                        userToBlock.is_blocked = true;
                    }
                    AppConnect.modelBD.SaveChanges();

                    // Сообщение о блокировке
                    if (userToBlock.is_blocked == true)
                    {
                        MessageBox.Show("Вы заблокированы. Обратитесь к администратору",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        ResetForm();
                        return;
                    }
                }

                int attemptsLeft = MaxAttempts - (userToBlock?.failed_attempts ?? 0);
                MessageBox.Show($"Вы ввели неверный логин или пароль. Пожалуйста проверьте ещё раз введенные данные. Осталось попыток: {attemptsLeft}",
                    "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);

                passwordBox.Clear();
                loginBox.Focus();
                return;
            }

            // Проверяем, не заблокирован ли пользователь
            if (_currentUser.is_blocked == true)
            {
                MessageBox.Show("Вы заблокированы. Обратитесь к администратору",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetForm();
                return;
            }

            // Если логин/пароль верны — сбрасываем счетчик попыток
            _currentUser.failed_attempts = 0;
            AppConnect.modelBD.SaveChanges();

            // Сохраняем данные для капчи
            _tempLogin = loginUser;
            _tempPassword = passwordUser;

            // Показываем панель с капчей
            authPanel.Visibility = Visibility.Collapsed;
            captchaPanel.Visibility = Visibility.Visible;

            _captchaFailedAttempts = 0;
            GenerateCaptcha();
            captchaInputBox.Focus();
        }

        private void GenerateCaptcha()
        {
            var random = new Random();
            _captchaCode = "";
            for (int i = 0; i < 4; i++)
                _captchaCode += random.Next(0, 10).ToString();

            captchaTextBlock.Text = _captchaCode;

            var colors = new[] {
                Brushes.DarkBlue, Brushes.DarkRed, Brushes.DarkGreen,
                Brushes.Purple, Brushes.Teal, Brushes.Orange
            };
            captchaTextBlock.Foreground = colors[random.Next(colors.Length)];
            captchaTextBlock.RenderTransform = new RotateTransform(random.Next(-15, 15));

            attemptsText.Text = $"Осталось попыток: {MaxAttempts - _captchaFailedAttempts}";
            captchaInputBox.Clear();
        }

        private void VerifyButton_Click(object sender, RoutedEventArgs e)
        {
            string captchaInput = captchaInputBox.Text.Trim();

            if (string.IsNullOrEmpty(captchaInput))
            {
                MessageBox.Show("Введите цифры с картинки!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                captchaInputBox.Focus();
                return;
            }

            // Обпвботкай неверной капчи
            if (captchaInput != _captchaCode)
            {
                _captchaFailedAttempts++;

                if (_captchaFailedAttempts >= MaxAttempts)
                {
                    // Блокируем пользователя в БД
                    _currentUser.is_blocked = true;
                    _currentUser.failed_attempts = MaxAttempts;
                    AppConnect.modelBD.SaveChanges();

                    MessageBox.Show("Вы заблокированы. Обратитесь к администратору",
                        "Блокировка", MessageBoxButton.OK, MessageBoxImage.Error);

                    ReturnToAuthForm();
                    return;
                }

                MessageBox.Show($"Неверная капча! Осталось попыток: {MaxAttempts - _captchaFailedAttempts}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                attemptsText.Text = $"Осталось попыток: {MaxAttempts - _captchaFailedAttempts}";
                GenerateCaptcha();
                captchaInputBox.Focus();
                return;
            }

            // Капча верна — завершаем авторизацию
            CompleteAuthorization();
        }

        private void CompleteAuthorization()
        {
            MessageBox.Show("Вы успешно авторизовались",
                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            var context = AppConnect.modelBD;
            var role = context.role.FirstOrDefault(r => r.role_id == _currentUser.role_id);

            if (role != null)
            {
                string roleName = role.role_name.ToLower();
                Window nextWindow;

                if (roleName.Contains("админ") || roleName.Contains("admin"))
                {
                    nextWindow = new AdminWindow.WindowAdmin();
                }
                else
                {
                    nextWindow = new UserWindow.WindowUser();
                }

                nextWindow.Show();
            }
            this.Close();
        }

        private void ReturnToAuthForm()
        {
            loginBox.Clear();
            passwordBox.Clear();
            captchaInputBox.Clear();
            authPanel.Visibility = Visibility.Visible;
            captchaPanel.Visibility = Visibility.Collapsed;
            loginBox.Focus();
        }

        private void ResetForm()
        {
            loginBox.Clear();
            passwordBox.Clear();
            captchaInputBox.Clear();
            authPanel.Visibility = Visibility.Visible;
            captchaPanel.Visibility = Visibility.Collapsed;
            _captchaFailedAttempts = 0;
            loginBox.Focus();
        }
    }
}
