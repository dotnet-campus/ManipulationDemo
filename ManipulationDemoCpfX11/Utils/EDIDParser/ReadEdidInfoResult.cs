namespace ManipulationDemoCpfX11.Utils;

public readonly record struct ReadEdidInfoResult(bool IsSuccess, string ErrorMessage, EdidInfo EdidInfo)
{
    public static ReadEdidInfoResult Success(EdidInfo edidInfo) => new ReadEdidInfoResult(true, "Success", edidInfo);
    public static ReadEdidInfoResult Fail(string errorMessage) => new ReadEdidInfoResult(false, errorMessage, default);
}