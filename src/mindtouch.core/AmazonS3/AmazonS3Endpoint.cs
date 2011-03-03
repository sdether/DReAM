using System.Collections.Generic;

namespace MindTouch.Dream.AmazonS3 {
    public class AmazonS3Endpoint {

        //--- Class Fields ---
        public static readonly AmazonS3Endpoint Default = new AmazonS3Endpoint("http://s3.amazonaws.com", null);
        public static readonly AmazonS3Endpoint USWest = new AmazonS3Endpoint("http://s3-us-west-1.amazonaws.com", "us-west-1");
        public static readonly AmazonS3Endpoint EU = new AmazonS3Endpoint("http://s3-eu-west-1.amazonaws.com", "EU");
        public static readonly AmazonS3Endpoint AsiaPacificSingapore = new AmazonS3Endpoint("http://s3-ap-southeast-1.amazonaws.com", "ap-southeast-1");
        public static readonly AmazonS3Endpoint AsiaPacificJapan = new AmazonS3Endpoint("http://s3-ap-northeast-1.amazonaws.com", "ap-northeast-1");
        private static IDictionary<string,AmazonS3Endpoint> _endpoints = new Dictionary<string,AmazonS3Endpoint>();

        //--- Class Constructor ----
        static AmazonS3Endpoint() {
            _endpoints.Add(Default.Name, Default);
            _endpoints.Add(USWest.Name, USWest);
            _endpoints.Add(EU.Name, EU);
            _endpoints.Add(AsiaPacificSingapore.Name, AsiaPacificSingapore);
            _endpoints.Add(AsiaPacificJapan.Name, AsiaPacificJapan);
        }

        //--- Class Methods ---
        public static AmazonS3Endpoint GetEndpoint(string name) {
            AmazonS3Endpoint endpoint;
            _endpoints.TryGetValue(name, out endpoint);
            return endpoint;
        }

        public static void AddEndpoint(AmazonS3Endpoint endpoint) {
            _endpoints[endpoint.Name] = endpoint;
        }

        //--- Fields ---
        public readonly XUri Uri;
        public readonly string LocationConstraint;
        public readonly string Name;

        //--- Constructors ---
        public AmazonS3Endpoint(string uri, string locationConstraint) {
            Uri = new XUri(uri);
            LocationConstraint = locationConstraint;
            Name = LocationConstraint ?? "default";
        }
    }
}