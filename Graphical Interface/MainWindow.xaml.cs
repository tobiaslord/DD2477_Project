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
using System.Diagnostics;


namespace Graphical_Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly User user;
        Book? CurrentBook;
        readonly SearchEngine engine;
        int currentBookDisplayRating;

        public MainWindow()
        {
            InitializeComponent();
            engine = new SearchEngine();
            user = new User{ id = "0" };
        }

        // Event handler for the Search button in the main view
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string searchTerm = SearchTextBox.Text;
            
            Search(searchTerm);
            
        }

        private int GetDisplayRating(Book b)
        {
            var rating = user.ratings.SingleOrDefault(r => r.bookId == b.id);
            if (rating is null)
                return 0;
            return rating.rating;
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
            if (user.ratings.SingleOrDefault(r => r.bookId == CurrentBook.id) is not null)
                return;

            Button button = sender as Button;
            int newRating = Grid.GetColumn(button) + 1;

            user.ratings.Add(new Rating
            {
                bookId = CurrentBook.id,
                rating = newRating,
                bookRatingCount = 1,
            });

            currentBookDisplayRating = GetDisplayRating(CurrentBook);
        }

        private void StarButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            int column = Grid.GetColumn(button);
            LightStars(column + 1);
        }

        private void StarButton_MouseLeave(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            int column = Grid.GetColumn(button);
            LightStars(currentBookDisplayRating);
        }

        private void LightStars(int nr)
        {
            for (int i = 0; i <= 4; i++)
            {
                Image img = GetStarImageByIndex(i);
                img.Source = new BitmapImage(new Uri("pack://application:,,,/black-star.png"));
            }

            for (int i = 0; i < nr; i++)
            {
                Image img = GetStarImageByIndex(i);
                img.Source = new BitmapImage(new Uri("pack://application:,,,/yellow-star.png"));
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
            currentBookDisplayRating = GetDisplayRating(CurrentBook);
            LightStars(currentBookDisplayRating);

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
            Stopwatch stopwatch = Stopwatch.StartNew();
            BookResultsGrid.ItemsSource = engine.GraphicSearch(searchTerm, user);
            stopwatch.Stop();

            TimeSpan elapsedTime = stopwatch.Elapsed;
            Debug.WriteLine("Elapsed time: " + elapsedTime);
        }
    }
}
