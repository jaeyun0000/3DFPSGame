using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호: 대문자, 소문자, 숫자, 기호를 각각 1개 이상 포함한 8~30 글자 (공백, 한글 불가)
    // 예) Abcdef1!, Zombie_2026
    private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{8,30}$");

    public string ErrorMessage => "비밀번호는 대문자, 소문자, 숫자, 기호를 모두 포함한 8~30글자여야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}
