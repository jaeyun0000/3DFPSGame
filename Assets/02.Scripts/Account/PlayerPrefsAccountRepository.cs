using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class PlayerPrefsAccountRepository
{
    // 계정 데이터 보관소
    private const string KeyPrefix = "Account_";
    private const string SaltKeySuffix = "_Salt";   // 이메일 도메인에는 '_'가 들어갈 수 없어서 아이디 키와 겹치지 않는다.
    private const int SaltSize = 16;                // 솔트 바이트 수

    private string GetKeyID(string id)
    {
        return KeyPrefix + id;
    }

    private string GetSaltKey(string id)
    {
        return GetKeyID(id) + SaltKeySuffix;
    }

    // 회원가입: 비밀번호는 그대로 저장하지 않고, 계정마다 다른 솔트를 붙여 해시한 값만 저장한다.
    public void Register(string id, string password)
    {
        string salt = CreateSalt();
        PlayerPrefs.SetString(GetSaltKey(id), salt);
        PlayerPrefs.SetString(GetKeyID(id), Hash(password, salt));
        PlayerPrefs.Save();
    }

    // 아이디 중복 검사
    public bool Exists(string id)
    {
        return PlayerPrefs.HasKey(GetKeyID(id));
    }

    // 비밀번호 검증
    public bool CheckPassword(string id, string password)
    {
        if (!Exists(id)) return false;

        string saved = PlayerPrefs.GetString(GetKeyID(id));

        // 해시를 도입하기 전에 가입한 계정은 평문으로 저장되어 있다. 맞으면 해시로 바꿔 다시 저장한다.
        if (!PlayerPrefs.HasKey(GetSaltKey(id)))
        {
            if (saved != password) return false;

            Register(id, password);
            return true;
        }

        return saved == Hash(password, PlayerPrefs.GetString(GetSaltKey(id)));
    }

    private string CreateSalt()
    {
        byte[] bytes = new byte[SaltSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes);
    }

    private string Hash(string password, string salt)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
            return Convert.ToBase64String(hash);
        }
    }
}
