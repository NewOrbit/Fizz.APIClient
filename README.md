# Fizz API client

## Introduction
When you work with Cashback offers in Fizz, you work at two levels. Firstly, there is a list of *retailers* that you can retrieve in full. You can then retrieve the details for an individual retailer, including the specific offers and conditions for that retailer.

In order to integrate to Fizz Benefits there are only a few things you need to do:
- Retrieve cash back offers in your server code and render them to your users.
  - Retrieve the full list for list and search purposes
  - Retrieve the full details of an individual offer when a user clicks through to that offer
- Create an authenticated link that the user can click on to be taken to the offer

There are no exposed API methods for managing users or retrieving data about activity or savings.  
Your users will automatically be created in Fizz when the user first clicks through on an offer meaning there is no need to set up any user synchronisation mechanism (See "Link to an offer" below).

Note that the retailer list and offers change frequently and you should avoid caching the offers for long. We recommend no more than one hour.

## Authentication
The authentication method used is a variation on two-legged oAuth 1.0. This is is particularly useful when creating the authenticated links that the end-user can click on, as they are simple GETs with no headers or cookies. For simplicity, the same authentication mechanism is used for the server-to-server communication.

You will be issued with a *key* and a *secret* when setting up the integration to Fizz. The *key* uniquely identifies your application and is not sensitive. The *secret* should never be transmitted.

In order to create an authenticated link, whether for server-to-server communication or for a link to show to an end-user, follow these steps:

(1) Create the full URL you want to use, for example `https://www.fizzbenefits.com/api/offers/cashback`

(2) Append the following parameters as query params:
- `key` - your unique key.
- `nonce` - a random string that should be different for each URL you create.
- `timestamp` - the current time expressed as UNIX Epoch time, i.e. the number of UTC seconds since 1970-01-01. Fizz will check the timestamp and will not allow URLs with a timestamp older than 60 minutes. Note that in most systems, only 5 minutes would be allowed but this much longer time makes it easier when you are embedding links in your user-facing pages.

You should end up with something like `https://www.fizzbenefits.com/api/offers/cashback?key=abcdefgh&nonce=1234567890&timestamp=1578390831`.

(3) You then need to append the secret to the URL and create a SHA256 hash of the whole string. In C# it looks something like this:
```csharp
private string GetSignature(string url)
{
    // Append the secret so it becomes part of the hash. This is essentially the authentication step.
    var signatureBase = url + this.secret; 

    using (var sha = SHA256.Create()) 
    {
        var mac = sha.ComputeHash(Encoding.ASCII.GetBytes(signatureBase));
        return Base64UrlEncoder.Encode(mac);
        
    }
}
```
The resulting hash is a byte array so you need to [Base64 URL encode](https://en.wikipedia.org/wiki/Base64#URL_applications) it so it can be included in the URL. Most languages have built-in functions for doing Base64 encoding, but you often have to write your own code to turn it into "modified Based64 for Url". Specifically, you must do this to the Base64 string:
- Remove the trailing `=` signs
- Replace `+` with `-`
- Replace `/` with `_`

In C# it looks like this:
```csharp
Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
```

Finally, add the signature as a final query param to the original URL like this:

```
https://www.fizzbenefits.com/api/offers/cashback?key=abc&nonce=1d05c2bf2ed14023bfb1206a383ecb14&timestamp=1578391536&signature=WqS1iv6I0-gYPPot-Jr3fCXGSVneJJV6BXo36zJulQk
```
*Note: This is a valid URL and valid signature created with the secret "123". You can use this to test your own implementation; if you get a different resulting URL from the same inputs then Fizz will reject it.*

### A note on URL encoding
Your query parameters should not need to include any non-ASCII characters and therefore there should be no need to do any URL encoding of any or all of the URL. This is also why it's safe to use the ASCII version of `GetBytes` rather than, say, UTF-8.  
The only special encoding required is the modification of the Base64 string to make it URL safe. You do need to make sure your `nonce` only includes ASCII characters, of course.  
In the unlikely event that you need to include other query parameters that may have non-URL-safe characters in them, you will need to URL encode *only* the value portion before you create the URL. For example, if you wanted to include something like `?myotherparameter=my sentence`, it should be URL encoded as `?myotherparameter=my%20sentence`. 

## Retrieve cash-back offers
You can call the API to retrieve the list of cash back offers and the detail of each offer.  
When retrieving offers, you will for all intents and purposes act like a normal Fizz user under the appropriate organisation in Fizz, meaning the offers will be filtered according to the normal system settings.  

The API calls must be made from your server-side code to avoid CORS and authentication considerations and to aid in appropriate caching. You may choose to pass the full response straight to your front-end for rendering. The Fizz front-end client stores the complete offer list in memory to enable in-memory searching and filtering on the client.  

We do recommend that you use some basic caching for these calls, with a duration of no more than one hour. We do not recommend that you store the offers in a database and synchronise them as that requires more complex logic to filter out expired and removed offers etc.  

### Retrieve the list of offers
In order to retrieve the full list of cash back offers, make a `GET` call to 
```
https://www.fizzbenefits.com/api/offers/cashback  
```
(with the appropriate authentication params appended).

The request will return a JSON array of retailer summaries, the categories for each retailer and a count of the number of offers for each retailer.

```json
[
    {
        "RetailerId": "e6a6e027-d8ae-4f3b-ae50-82b84cbd06aa",
        "Name": "Expedia",
        "Disabled": false,
        "ThumbnailImageUrl": "https://d2t2wfirfyzjhs.cloudfront.net/images/suppliers/large-logos/expedianewl.png",
        "CashbackOfferCount": 7,
        "TotalOfferCount": 7,
        "TagList": [
            "8",
            "8/50",
            "64"
        ]
    },
    ....
]
```
*Note: Do not display offers when `Disabled` is `true`.

### Retrieve the list of categories
Cash back offers are organised into hierarchical categories that you may expose to end-users in order to help them identify offers.

In order to retrieve the current categories, make a `GET` call to 
```
https://www.fizzbenefits.com/api/offers/cashback/categories 
```
(with the appropriate authentication params appended).

### Filter retailers by category
The categories are hierarchical. For example, you may have "Travel", underneath which you have "Holidays".  
Each retailer has one or more category hierarchy IDs in the `TagList` array. For example, assuming the "Travel" category has ID 8 and "Holidays" (which is underneath that) has ID 50 then an offer specifically for Holidays will have "8/50/" in a Tag. If you want to find all Travel offers, find all offers where the hierarchy ID starts with "8/". If you want only holidays, look for offers where the hierarchy ID starts with "8/50/".  

### Retrieve the details of an individual offer
The retailer list only contains enough information to list and search offers. When a users clicks on a retailer to see more details, you need to make another call to retrieve the full details of the retailer, including the specific offers, so you can display it to the user.

In order to retrieve the details of a specific cash back offer, make a GET call to 
```
/api/offers/cashback/{retailerId}.
```
(with the appropriate authentication query parameters)

```JSON
{
    "Offers": [
        {
            "Id": 423207,
            "Title": "Flights",
            "CashbackDetails": {
                "Type": 2,
                "Value": 0.9
            },
            "EarnDetail": ""
        },
        {
            "Id": 428876,
            "Title": "Hotel Booking of 2 nights or more",
            "CashbackDetails": {
                "Type": 2,
                "Value": 7.2
            },
            "EarnDetail": ""
        },
        {
            "Id": 428937,
            "Title": "Hotel Booking of 1 night",
            "CashbackDetails": {
                "Type": 2,
                "Value": 5.4
            },
            "EarnDetail": ""
        },
        {
            "Id": 430874,
            "Title": "Experiences",
            "CashbackDetails": {
                "Type": 2,
                "Value": 10.8
            },
            "EarnDetail": "Without Using Code Online"
        },
        {
            "Id": 430875,
            "Title": "Ground Transportation/Shuttles/Transfers",
            "CashbackDetails": {
                "Type": 2,
                "Value": 9.9
            },
            "EarnDetail": "Excludes train bookings"
        },
        {
            "Id": 430876,
            "Title": "Car Hire",
            "CashbackDetails": {
                "Type": 2,
                "Value": 9.0
            },
            "EarnDetail": ""
        },
        {
            "Id": 430877,
            "Title": "All Other Packages",
            "CashbackDetails": {
                "Type": 2,
                "Value": 3.6
            },
            "EarnDetail": "Flight + Hotel / Flight + Car / Flight + Hotel + Car / Hotel + Car bookings"
        }
    ],
    "Description": "Get a great deal on flights, hotels and holiday packages at Expedia, and save even more money with our cashback and discount code deals. Search more than 500,000 hotels and 400 airlines worldwide. Whether you are planning a beach holiday or city break, get a great deal with these offers and discount codes. Stay in London, Edinburgh or Dublin, or jet off to New York, Dubai, Egypt, Thailand or Mexico. Browse last-minute deals, Eurostar offers and ski holidays, while you can also book car hire.",
    "RetailerInfo": [
        {
            "HeaderTitle": "What will stop me getting cashback?",
            "Details": [
                {
                    "Text": "Using a promotional/voucher code not posted and approved by Fizz Benefits.",
                    "SortOrder": 1
                },
                {
                    "Text": "If you make a cottage booking",
                    "SortOrder": 1
                },
                {
                    "Text": "If you make a booking for Cuba",
                    "SortOrder": 1
                },
                {
                    "Text": "If you use nectar points to pay.",
                    "SortOrder": 1
                }
            ]
        },
        {
            "HeaderTitle": "Good to know",
            "Details": [
                {
                    "Text": "Expedia cashback can be earned simply by clicking through to the merchant and shopping as normal.",
                    "SortOrder": 1
                },
                {
                    "Text": "Expedia Cashback is available through Fizz Benefits on genuine, tracked transactions completed immediately and wholly online.",
                    "SortOrder": 1
                },
                {
                    "Text": "Recurrences on the amount of purchases that can be made while earning cashback may be limited.",
                    "SortOrder": 1
                },
                {
                    "Text": "An ePackage is a flight and hotel booked in the same transaction. A holiday is defined as a package holiday",
                    "SortOrder": 1
                },
                {
                    "Text": "Unless otherwise stated expect transactions for this retailer to appear within 24 hours.",
                    "SortOrder": 1
                },
                {
                    "Text": "Cashback is not paid until your stay has been completed.",
                    "SortOrder": 1
                }
            ]
        },
        {
            "HeaderTitle": "What to do when",
            "Details": [
                {
                    "Text": "Some merchants may not be forthcoming with untracked cashback.  We endeavour to chase untracked cashback but reserve the right to halt enquiries at any time.  Please do not make purchase decisions based upon expected cashback as it is not guaranteed.",
                    "SortOrder": 1
                },
                {
                    "Text": "The vast majority of transactions from merchants track successfully, occasionally a transaction may not get reported.  If you believe this to be the case, please submit a \"Missing Cashback\" query within 100 days of the transaction, we will be unable to chase up claims older than this.",
                    "SortOrder": 1
                },
                {
                    "Text": "Please ensure any claims for untracked cashback are raised as soon as the stay/flights in the booking have been completed, as this merchant can only review claims for 3 months once the stay/flight dates have passed.",
                    "SortOrder": 1
                }
            ]
        },
        {
            "HeaderTitle": "What else is essential?",
            "Details": [
                {
                    "Text": "Purchases must be made through Expedia's UK site in order to be tracked",
                    "SortOrder": 1
                }
            ]
        }
    ],
    "RetailerId": "d8ae-4f3b-ae50-82b84cbd06aa",
    "Name": "Expedia",
    "Disabled": false,
    "ThumbnailImageUrl": "https://d2t2wfirfyzjhs.cloudfront.net/images/suppliers/large-logos/expedianewl.png",
    "CashbackOfferCount": 7,
    "TotalOfferCount": 7,
    "TagList": [
        "8",
        "8/50",
        "64"
    ]
}
```

For each *offer*, under Cashback Details the Type can have the following values:
1 = Pounds
2 = Percent

## Link to an offer
When a user clicks on an offer, you need to create a URL and redirect the user to this URL.
The format should be 
```
https://www.fizzbenefits.com/useoffer/cashback/{offerId}/{userId}?key={key}&nonce={nonce}&timestamp={timestamp}&signature={hmac}
```
Key, nonce, timestamp and signature are as explained in the authentication section.  

The `userId` is this end-users unique ID in your system. A Fizz user will automatically be created for this userId in the Fizz system the first time the user clicks on it. The user creation in Fizz is asynchronous so will not noticeably impact the user's journey.

Do note that the links will only be valid for one hour due to the timestamp.
