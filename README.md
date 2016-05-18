# Simple Token Provider Middleware for ASP.NET

This project demonstrates how to generate [JSON Web Tokens](https://en.wikipedia.org/wiki/JSON_Web_Token) (JWTs) for token authentication in ASP.NET Core RC2. The functionality is wrapped up in a reusable middleware component.

This has **not been tested in production**, so explore and use at your own risk!

## Configuring the middleware

The token provider endpoint can be added to your pipeline in `Configure()`:

```csharp
app.UseSimpleTokenProvider(new TokenProviderOptions
{
    Path = "/api/token",
    Audience = "ExampleAudience",
    Issuer = "ExampleIssuer",
    SigningCredentials = signingCredentials,
    IdentityResolver = GetIdentity
});
```

The options are:

* **Path** (optional) - The endpoint path relative to the server root. Default: `/token`
* **Audience** - The JWT `aud` claim value.
* **Issuer** - The JWT `iss` claim value.
* **Expiration** (optional) - The expiration duration for new tokens. Default: 5 minutes
* **SigningCredentials** - The signing credentials to use when signing new tokens.
* **IdentityResolver** - A delegate that takes a username/password and returns a `ClaimsIdentity` if the user exists, or `null` if the user does not exist.
* **NonceGenerator** (optional) - A delegate that generates a random value (nonce) for each new token. Default: `Guid.NewGuid()`

If you are using an HMAC-SHA256 key (symmetric signing), the `SigningCredentials` will look like:

```csharp
// The secret key every token will be signed with.
// Keep this safe on the server!
var secretKey = "mysupersecret_secretkey!123";

var signingCredentials = new SigningCredentials(
    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
    SecurityAlgorithms.HmacSha256);
```

The `IdentityResolver` delegate abstracts away the concern of looking up and verifying a user given a username and password. If the user exists and the password is valid, a `ClaimsIdentity` should be returned. If not, the delegate should return null.

You can use the following dummy resolver for testing: **(don't use in production!)**

```csharp
private Task<ClaimsIdentity> GetIdentity(string username, string password)
{
    // Don't do this in production, obviously!
    if (username == "TEST" && password == "TEST123")
    {
        return Task.FromResult(new ClaimsIdentity(new GenericIdentity(username, "Token"), new Claim[] { }));
    }

    // Credentials are invalid, or account doesn't exist
    return Task.FromResult<ClaimsIdentity>(null);
}
```

## How it works

At a high level, the middleware does the following:

* Intercepts requests to `options.Path`
* Verifies the request is a POST with `Content-Type: application/x-www-form-urlencoded`
* Pulls the username and password out of the form body
* Delegates to `options.IdentityResolver` to look up the user; errors if the credentials are bad
* Creates a JWT with the following claims:
  * `sub` (subject) - the username
  * `jti` (nonce) - a random value
  * `iat` (issued-at) - the current time
  * `nbf` (not-before) - the current time
  * `exp` (expiration) - the current time + `options.Expiration`
  * `iss` (issuer) - `options.Issuer`
  * `aud` (audience) - `options.Audience`
* Encodes the JWT to a string and sends it back to the client

## Trying it out

You can install the middleware in a new project, or just run the included test project. Send a POST request using a tool like Fiddler or Postman:

```
POST /token (or whatever you set options.Path to)
Content-Type: application/x-www-form-urlencoded

username=TEST&password=TEST123
```

You should get a `200 OK` response:

```
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJURVNUIiwianRpIjoiYzRjYzdhMmUtMjI0OS00ZWUzLWJkM2MtYzU5MDkzYmU5MGU1IiwiaWF0IjoxNDYzNTMwMDI0LCJuYmYiOjE0NjM1MzAwMjMsImV4cCI6MTQ2MzUzMDMyMywiaXNzIjoiRXhhbXBsZUlzc3VlciIsImF1ZCI6IkV4YW1wbGVBdWRpZW5jZSJ9.mI0NPO437IuBSt5kmayy5XhNFEHVF4IyMkKsmtas6w8",
  "expires_in": 300
}
```

You can try decoding and verifying the JWT at [jsonwebtoken.io](https://jsonwebtoken.io).

## Acknowledgements

These resources were extremely helpful as I was figuring out how to make this work:

* https://github.com/mrsheepuk/ASPNETSelfCreatedTokenAuthExample
* http://stackoverflow.com/questions/29048122/token-based-authentication-in-asp-net-5-vnext
