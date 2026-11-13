namespace HttpFromScratch.Stack.Errors;

public enum IpHeaderParseError
{
    InvalidPacketLength,
    InvalidHeaderLength,
    InvalidVersion,
}