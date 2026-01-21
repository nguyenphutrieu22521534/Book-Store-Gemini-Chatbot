using Microsoft.EntityFrameworkCore;

namespace BookMart.Repositories
{
    public class HomeRepository : IHomeRepository
    {

        private readonly ApplicationDbContext _db;

        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _db.Genres.ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooks(string sTerm = "", int genreId = 0)
        {


            //sTerm = sTerm.ToLower();
            //IEnumerable<Book> books = await (from book in _db.Books
            //                                 join genre in _db.Genres
            //                                 on book.GenreId equals genre.Id
            //                                 where string.IsNullOrWhiteSpace(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
            //                                 select new Book
            //                                 {
            //                                     Id = book.Id,
            //                                     Image = book.Image,
            //                                     AuthorName = book.AuthorName,
            //                                     BookName = book.BookName,
            //                                     GenreId = book.GenreId,
            //                                     Price = book.Price,
            //                                     GenreName = genre.GenreName
            //                                     //Quantity = bookWithStock == null ? 0 : bookWithStock.Quantity
            //                                 }
            //             ).ToListAsync();

            sTerm = sTerm.ToLower();
            IEnumerable<Book> books = await (from book in _db.Books
                                             join genre in _db.Genres
                                             on book.GenreId equals genre.Id
                                             join stock in _db.Stocks
                                             on book.Id equals stock.BookId
                                             into book_stocks
                                             from bookWithStock in book_stocks.DefaultIfEmpty()
                                             where string.IsNullOrWhiteSpace(sTerm) || (book != null && book.BookName.ToLower().StartsWith(sTerm))
                                             select new Book
                                             {
                                                 Id = book.Id,
                                                 Image = book.Image,
                                                 AuthorName = book.AuthorName,
                                                 BookName = book.BookName,
                                                 GenreId = book.GenreId,
                                                 Price = book.Price,
                                                 GenreName = genre.GenreName,
                                                 Quantity = bookWithStock == null ? 0 : bookWithStock.Quantity
                                             }
                         ).ToListAsync();
            if (genreId > 0)
            {

                books = books.Where(a => a.GenreId == genreId).ToList();
            }

            // refactored code
            // In this code we are first building query, then rebuilding that query on the basis of filter. Query is translated into sql when we call .ToListAsync() method.

            //var bookQuery = _db.Books
            //   .AsNoTracking()
            //   .Include(x => x.Genre)
            //   .Include(x => x.Stock)
            //   .AsQueryable();

            //if (!string.IsNullOrWhiteSpace(sTerm))
            //{
            //    bookQuery = bookQuery.Where(b => b.BookName.StartsWith(sTerm.ToLower()));
            //}

            //if (genreId > 0)
            //{
            //    bookQuery = bookQuery.Where(b => b.GenreId == genreId);
            //}

            //var books = await bookQuery
            //    .AsNoTracking()
            //    .Select(book => new Book
            //    {
            //        Id = book.Id,
            //        Image = book.Image,
            //        AuthorName = book.AuthorName,
            //        BookName = book.BookName,
            //        GenreId = book.GenreId,
            //        Price = book.Price,
            //        GenreName = book.Genre.GenreName,
            //        Quantity = book.Stock == null ? 0 : book.Stock.Quantity
            //    }).ToListAsync();
            return books;

        }
    }
}


