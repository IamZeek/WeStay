﻿using WeStay.ListingService.Models;

namespace WeStay.ListingService.Models.Requests
{
    public class SearchListingsRequest
    {
        public string Location { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? Guests { get; set; }
        public int? Bedrooms { get; set; }
        public int? Beds { get; set; }
        public int? Bathrooms { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public ListingType? Type { get; set; }
        public List<int> AmenityIds { get; set; } = new List<int>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "createdAt";
        public bool SortDescending { get; set; } = true;
    }
}