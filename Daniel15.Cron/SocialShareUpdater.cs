﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Daniel15.BusinessLayer.Services.Social;
using Daniel15.Data.Entities.Blog;
using Daniel15.Data.Repositories;
using Daniel15.Shared.Extensions;
using Newtonsoft.Json;

namespace Daniel15.Cron
{
	/// <summary>
	/// Handles updating of social media sharing counts
	/// </summary>
	public class SocialShareUpdater
	{
		private readonly ISocialManager _socialManager;
		private readonly IBlogRepository _blogRepository;
		private readonly HttpClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="SocialShareUpdater" /> class.
		/// </summary>
		/// <param name="socialManager">The social manager.</param>
		/// <param name="blogRepository">The blog repository.</param>
		public SocialShareUpdater(ISocialManager socialManager, IBlogRepository blogRepository, HttpClient client)
		{
			_socialManager = socialManager;
			_blogRepository = blogRepository;
			_client = client;
		}

		/// <summary>
		/// Updates the social media sharing counts of all blog posts
		/// </summary>
		public async Task RunAsync()
		{
			var posts = _blogRepository.LatestPosts(10000);
			foreach (var post in posts)
			{
				await UpdatePostAsync(post);
			}
		}

		/// <summary>
		/// Updates the social media sharing counts of the specified post
		/// </summary>
		/// <param name="post">Post to update</param>
		private async Task UpdatePostAsync(PostModel post)
		{
			// TODO: Move this elsewhere (Business Layer)
			var urls = await GetBlogUrlAsync(post);
			var counts = await _socialManager.ShareCountsAsync(
				post, 
				urls.Url, 
				urls.ShortUrl
			);
			post.ShareCounts = counts.ToDictionary(x => x.Key.Id, x => x.Value);
			_blogRepository.Save(post);

			var total = counts.Sum(x => x.Value);
			Console.WriteLine("Updated '{0}' ({1} shares in total)", post.Title, total);
		}

		/// <summary>
		/// Gets the URL to the specified blog post
		/// </summary>
		/// <param name="post">Blog post to get URL of</param>
		/// <returns>URL of the blog post</returns>
		private async Task<UrlResponse> GetBlogUrlAsync(PostModel post)
		{
			// TODO: Remove hard-coded URL from here
			var urlApi = $"https://dan.cx/api/posts/{post.Id}/url";
			return await _client.GetJsonAsync<UrlResponse>(urlApi);
			
		}

		[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
		[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
		private class UrlResponse
		{
			public string Url { get; set; }
			public string ShortUrl { get; set; }
		}
	}
}
