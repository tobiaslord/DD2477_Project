using ElasticSearchNamespace;
using Models;
using PlaywrightTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
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
using Book = Models.SimpleBook;
using User = Models.SimpleUser;
using Rating = Models.Rating;


namespace Graphical_Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        User user = new User();
        Book CurrentBook;
        ElasticIndex searcher;

        public MainWindow()
        {
            InitializeComponent();
            var root = Directory.GetCurrentDirectory();
            var dotenv = System.IO.Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            searcher = new ElasticIndex();
            user.id = "0";
        }

        // Event handler for the Search button in the main view
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string searchTerm = SearchTextBox.Text;
            Search(searchTerm);
        }

        // Event handler for clicking a search result
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BookDetailsGrid.Visibility = Visibility.Collapsed;
            BookResultsGrid.Visibility = Visibility.Visible;
            SearchTextBox.Visibility = Visibility.Visible;
        }

        // Event handler for clicking a search result
        private void StarButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int newRating = Grid.GetColumn(button) + 1;

            user.ratings.Add(new Rating
            {
                bookId = CurrentBook.id,
                rating = newRating,
                bookRatingCount = 1,
            });
            // RateBook(CurrentBook, newRating);
        }

        private void StarButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            int column = Grid.GetColumn(button);

            for (int i = 0; i <= column; i++)
            {
                Image img = GetStarImageByIndex(i);
                img.Source = new BitmapImage(new Uri("pack://application:,,,/yellow-star.png"));
            }
        }

        private void StarButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            int column = Grid.GetColumn(button);

            for (int i = 0; i <= column; i++)
            {
                Image img = GetStarImageByIndex(i);
                img.Source = new BitmapImage(new Uri("pack://application:,,,/black-star.png"));
            }
        }

        private Image GetStarImageByIndex(int index)
        {
            return index switch
            {
                0 => StarOneButtonImg,
                1 => StarTwoButtonImg,
                2 => StarThreeButtonImg,
                3 => StarFourButtonImg,
                4 => StarFiveButtonImg,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        // Event handler for clicking a search result
        private void ClickBook(object sender, MouseButtonEventArgs e)
        {
            // Get the Book object that was clicked
            Book book = (Book)((Border)sender).DataContext;
            CurrentBook = book;

            // Set the image source and description text of the BookDetailsImage and BookDetailsDescription elements
            BookDetailsImage.Source = new BitmapImage(new Uri(book.imageUrl));
            BookDetailsDescription.Text = book.description;
            BookDetailsAuthor.Text = string.Join(", ", book.authors);
            BookDetailsGenres.Text = string.Join(", ", book.genres);

            SearchTextBox.Visibility = Visibility.Collapsed;
            BookResultsGrid.Visibility = Visibility.Collapsed;
            BookDetailsGrid.Visibility = Visibility.Visible;
        }

        // Method to perform a search and display the results in the book list
        private void Search(string searchTerm)
        {
            BookResultsGrid.ItemsSource = searcher.GraphicSearch(searchTerm, user);
        }
    }
}
