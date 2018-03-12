﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Examine;
using Examine.LuceneEngine.Providers;
using Examine.LuceneEngine.SearchCriteria;
using Umbraco.Web.WebApi;
using UmbracoExamine;

namespace OurUmbraco.Community.Map
{
    public class MapApiController : UmbracoApiController
    {
        //swLat= 44.255278708039896
        //neLat= 51.599464320768725

        //swLon= -142.07023127555988
        //neLon= -124.49210627555988

        [HttpGet]
        public List<MemberLocation> GetAllMemberLocations(string swLat, string swLon, string neLat, string neLon)
        {           
            var memberSearcher = ExamineManager.Instance.SearchProviderCollection["InternalMemberSearcher"];
            var searchCritera = memberSearcher.CreateSearchCriteria(IndexTypes.Member);

            var longSwLat = Convert.ToDouble(swLat);
            var longSwLon = Convert.ToDouble(swLon);
            var longNeLat = Convert.ToDouble(neLat);
            var longNeLon = Convert.ToDouble(neLon);
            
            //Find all active members - where a lat & lon is set (with new NodeGathering field to index)
            var query = searchCritera.Field("hasLocationSet", "1")
                .And().Field("blocked", "0")
                .And().Field("umbracoMemberApproved", "1")
                .And().Range("latitudeNumber", longSwLat, longNeLat, true, true)
                .And().Range("longitudeNumber", longSwLon, longNeLon, true, true)
                .Not().Range(LuceneIndexer.SortedFieldNamePrefix + "karma", 0, 70, true, true) //The raw field  in the index has the magic prefix
                .And().OrderByDescending(new SortableField("karma", SortType.Int)) //When you use a sortable field - you need to not include the magic string prefix - as Examine adds it in
                .Compile();
            
            var results = memberSearcher.Search(query);

            //Pluck the fields we need from the Examine index fields
            //For our much simpler model to send back as the JSON response
            var members = results.Select(result => new MemberLocation
            {
                Id = result.Id,
                Name = result.Fields["nodeName"],
                Avatar = result.Fields["avatar"],
                Lat = result.Fields["latitude"],
                Lon = result.Fields["longitude"],
                Karma = Convert.ToInt32(result.Fields[LuceneIndexer.SortedFieldNamePrefix + "karma"]) //Have to access the value as the raw field name with the magic string prefix
            })
            .ToList();
            
            return members;
        }

    }
}