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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;


using InTouch.Model;
using InTouch.ViewModel;
using InTouch.NetWork;
using MaterialDesignThemes.Wpf;

namespace InTouch {
    /// <summary>
    /// addressListPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddressListPage : Page {
        private AddressBookViewModel viewModel;
        public AddressListPage() {
            InitializeComponent();
            viewModel = new AddressBookViewModel(App.addressBook);
            contactList.ItemsSource = viewModel.addressBook.items;
        }

        private void ContactList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int index = contactList.SelectedIndex;

            if (index == -1) {
                return;
            }
            AddressBook.Item itemToShow = viewModel.addressBook.items[index];

            showStack.Children.Clear();
            showStack.VerticalAlignment = VerticalAlignment.Center;
            showStack.HorizontalAlignment = HorizontalAlignment.Center;

            Type type = itemToShow.GetType();

            foreach (PropertyInfo pi in type.GetProperties()) {
                string name = pi.Name;
                var value = pi.GetValue(itemToShow, null);
                var dockPanel = new DockPanel();
                dockPanel.Children.Add(new Label() { Content = $"{name}: ", MinWidth = 100 });
                dockPanel.Children.Add(new Label() { Content = value != null ? value.ToString() : "null", MinWidth = 100 });
                showStack.Children.Add(dockPanel);
            }
           
        }

        private void NewFriend_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            showStack.Children.Clear();
            showStack.VerticalAlignment = VerticalAlignment.Center;
            showStack.HorizontalAlignment = HorizontalAlignment.Center;

            DockPanel dockPanelName = new DockPanel();
            dockPanelName.Children.Add(new PackIcon() {
                Kind = PackIconKind.UserAdd,
                VerticalAlignment = VerticalAlignment.Center,
                MinHeight = 30,
                MinWidth = 30,
                Margin = new Thickness(10, 0, 10, 0)
            });
            var nameTbx = new TextBox() { MinWidth = 100 };
            HintAssist.SetHint(nameTbx, "待查找用户名");
            dockPanelName.Children.Add(nameTbx);

            DockPanel dockPanelAlias = new DockPanel();
            dockPanelAlias.Children.Add(new PackIcon() {
                Kind = PackIconKind.RenameBox,
                VerticalAlignment = VerticalAlignment.Center,
                MinHeight = 30,
                MinWidth = 30,
                Margin = new Thickness(10, 0, 10, 0)
            });
            var AliasTbx = new TextBox() { MinWidth = 100 };
            HintAssist.SetHint(AliasTbx, "备注");
            dockPanelAlias.Children.Add(AliasTbx);


            var dockPanelBtn = new DockPanel() { Margin = new Thickness(0, 15, 0, 0) };
            var addBtn = new Button() { Content = "添加", MaxWidth = 60 };
            addBtn.SetBinding(Button.TagProperty, new Binding("Text") { Source = nameTbx });
            addBtn.SetBinding(Button.ToolTipProperty, new Binding("Text") { Source = AliasTbx });
            addBtn.Click += AddBtn_Click;
            dockPanelBtn.Children.Add(addBtn);
            var queryBtn = new Button() { Content = "查询", MaxWidth = 60 };
            queryBtn.SetBinding(Button.TagProperty, new Binding("Text") { Source = nameTbx });
            queryBtn.Click += QueryBtn_Click;
            dockPanelBtn.Children.Add(queryBtn);


            showStack.Children.Add(dockPanelName);
            showStack.Children.Add(dockPanelAlias);
            showStack.Children.Add(dockPanelBtn);

            // pageShow.Content = stackPanel;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e) {

            var btn = (Button)sender;
            string recv = CSClient.getInstance().SendAMsg($"q{(string)btn.Tag}");
            string Alias = (string)btn.ToolTip;
            if (Alias == null || Alias.Length == 0)
                Alias = (string)btn.Tag;
            switch (recv) {
                case "error":
                    MessageBox.Show("添加失败");
                    break;
                case "Please send the correct message.":
                    MessageBox.Show("不存在此用户");
                    break;
                default:
                    foreach (var item in viewModel.addressBook.items) {
                        if (item.UserName == (string)btn.Tag) {
                            MessageBox.Show("此用户已经是您的好友");
                            return;
                        }
                    }
                    Model.AddressBook.Item newItem = new AddressBook.Item() {
                        Alias = Alias,
                        UserName = (string)btn.Tag,
                        isOnline = (recv != "n"),
                        IPAddress = recv != "n" ? recv : ""
                    };
                    viewModel.addressBook.items.Add(newItem);
                    MessageBox.Show("添加成功");

                    // 更新界面
                    contactList.ItemsSource = null;
                    contactList.ItemsSource = viewModel.addressBook.items;
                    break;
            }

        }

        private void QueryBtn_Click(object sender, RoutedEventArgs e) {
            var btn = (Button)sender;
            string recv = CSClient.getInstance().SendAMsg($"q{(string)btn.Tag}");
            switch (recv) {
                case "error":
                    MessageBox.Show("添加失败");
                    break;
                case "Please send the correct message.":
                    MessageBox.Show("不存在此用户");
                    break;
                case "n":
                    MessageBox.Show("用户已注册但不在线");
                    break;
                default:
                    MessageBox.Show($"用户主机位于{recv}");
                    break;
            }

        }

        private void Update_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            App.QueryAllitem();
            contactList.ItemsSource = null;
            contactList.ItemsSource = viewModel.addressBook.items;
        }

        private void NewGroup_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            groupChat.newGroup(groupUserTbx.Text);
            
        }

        private void Delete_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if(contactList.SelectedIndex == -1) {
                return;
            }

            App.addressBook.items.RemoveAt(contactList.SelectedIndex);
        }
    }
}
