using UnityEngine;

public static class SaveSystem
{
    private const string ChaveCena = "save_cena";
    private const string ChaveX = "save_x";
    private const string ChaveY = "save_y";
    private const string ChaveZ = "save_z";

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(ChaveCena);
    }

    public static void SaveGame(string cena, Vector3 posicao)
    {
        PlayerPrefs.SetString(ChaveCena, cena);
        PlayerPrefs.SetFloat(ChaveX, posicao.x);
        PlayerPrefs.SetFloat(ChaveY, posicao.y);
        PlayerPrefs.SetFloat(ChaveZ, posicao.z);
        PlayerPrefs.Save();
    }

    public static string LoadScene()
    {
        return PlayerPrefs.GetString(ChaveCena);
    }

    public static Vector3 LoadPosition()
    {
        return new Vector3(
            PlayerPrefs.GetFloat(ChaveX),
            PlayerPrefs.GetFloat(ChaveY),
            PlayerPrefs.GetFloat(ChaveZ));
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteAll();
    }
}