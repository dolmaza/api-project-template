namespace ProjectName.Domain.Common;

public sealed record Money(string Currency, long Minor);
public sealed record GeoPoint(double Lat, double Lng);
public sealed record Address(string Country, string City, string Street, string? Number);
