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


namespace Graphical_Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Book CurrentBook;
        ElasticIndex searcher;

        public MainWindow()
        {
            InitializeComponent();
            var root = Directory.GetCurrentDirectory();
            var dotenv = System.IO.Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            searcher = new ElasticIndex();
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

            SearchTextBox.Visibility = Visibility.Collapsed;
            BookResultsGrid.Visibility = Visibility.Collapsed;
            BookDetailsGrid.Visibility = Visibility.Visible;
        }

        // Method to perform a search and display the results in the book list
        private void Search(string searchTerm)
        {
            // List<Book> matchedBooks = search(searchTerm);
            //List<Book> matchedBooks = new List<Book>();

            //var matchedBooks = new List<SimpleBook>
            //{
            //    new SimpleBook
            //    {
            //        id = "1",
            //        bookId = "1",
            //        author = "Author One",
            //        title = "Book One",
            //        description = "Description for Book One",
            //        imageUrl = "https://images-na.ssl-images-amazon.com/images/S/compressed.photo.goodreads.com/books/1622355533i/4667024.jpg",
            //        authorUrl = "https://example.com/author1",
            //        rating = 4.5f,
            //        ratingCount = 1000,
            //        reviewCount = 250,
            //        genres = new List<string> { "Mystery", "Thriller" },
            //        authors = new List<string> { "Author One, A. Andersson" },
            //        authorUrls = new List<string> { "https://example.com/author1" }
            //    },
            //    new SimpleBook
            //    {
            //        id = "2",
            //        bookId = "2",
            //        author = "Author Two",
            //        title = "Book Two with a really long name",
            //        description = "Description for Book Two",
            //        imageUrl = "https://images-na.ssl-images-amazon.com/images/S/compressed.photo.goodreads.com/books/1631251689i/4214.jpg",
            //        authorUrl = "https://example.com/author2",
            //        rating = 3.8f,
            //        ratingCount = 800,
            //        reviewCount = 180,
            //        genres = new List<string> { "Fantasy", "Adventure" },
            //        authors = new List<string> { "Author Two" },
            //        authorUrls = new List<string> { "https://example.com/author2" }
            //    },
            //    new SimpleBook
            //    {
            //        id = "3",
            //        bookId = "3",
            //        author = "Author Three",
            //        title = "Book Three",
            //        description = "Description for Book Two",
            //        imageUrl = "https://images-na.ssl-images-amazon.com/images/S/compressed.photo.goodreads.com/books/1442375726i/7366.jpg",
            //        authorUrl = "https://example.com/author2",
            //        rating = 3.8f,
            //        ratingCount = 800,
            //        reviewCount = 180,
            //        genres = new List<string> { "Fantasy", "Adventure" },
            //        authors = new List<string> { "Author Two" },
            //        authorUrls = new List<string> { "https://example.com/author2" }
            //    },
            //};

            

            BookResultsGrid.ItemsSource = searcher.BetterSearch(searchTerm);

        }
    }
}
