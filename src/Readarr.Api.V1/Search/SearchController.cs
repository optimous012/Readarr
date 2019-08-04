using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using Readarr.Api.V1.Author;
using Readarr.Api.V1.Books;
using Readarr.Http;

namespace Readarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;
        private readonly IBuildFileNames _fileNameBuilder;

        public SearchController(ISearchForNewEntity searchProxy, IBuildFileNames fileNameBuilder)
        {
            _searchProxy = searchProxy;
            _fileNameBuilder = fileNameBuilder;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            var id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Books.Author author)
                {
                    resource.Author = author.ToResource();
                    resource.ForeignId = author.ForeignAuthorId;

                    var poster = author.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                    if (poster != null)
                    {
                        resource.Author.RemotePoster = poster.Url;
                    }
                }
                else if (result is NzbDrone.Core.Books.Book book)
                {
                    resource.Book = book.ToResource();
                    resource.Book.Overview = book.Editions.Value.Single(x => x.Monitored).Overview;
                    resource.Book.Author = book.Author.Value.ToResource();
                    resource.Book.Editions = book.Editions.Value.ToResource();
                    resource.ForeignId = book.ForeignBookId;

                    var cover = book.Editions.Value.Single(x => x.Monitored).Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);
                    if (cover != null)
                    {
                        resource.Book.RemoteCover = cover.Url;
                    }

                    resource.Book.Author.Folder = _fileNameBuilder.GetAuthorFolder(book.Author);
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
