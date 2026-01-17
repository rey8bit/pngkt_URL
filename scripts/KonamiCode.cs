using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KonamiCode : UdonSharpBehaviour
{
    // Define the Konami code sequence as integers
    private readonly int[] konamiCode = { 0, 0, 1, 1, 2, 3, 2, 3, 4, 5, 6 }; //Progres input
    private int currentIndex = 0;

    public AudioSource audioSource; //Audiosourcenya
    public AudioClip successClip; //Suara berhasil

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CheckKonamiCode(0);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CheckKonamiCode(1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CheckKonamiCode(2);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CheckKonamiCode(3);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            CheckKonamiCode(4);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            CheckKonamiCode(5);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            CheckKonamiCode(6);
        }
    }

    private void CheckKonamiCode(int inputIndex)
    {
        if (inputIndex == konamiCode[currentIndex])
        {
            currentIndex++;

            // Pengecekan jika input berhasil
            if (currentIndex >= konamiCode.Length)
            {
                // Jika berhasil
                Debug.Log("Kode Konami berhasil dimasukkan!");
                PlaySuccessAudio();
                currentIndex = 0;
            }
        }
        else
        {
            //Ulang jika salah
            currentIndex = 0;
        }
    }

    private void PlaySuccessAudio()
    {
        if (audioSource != null && successClip != null)
        {
            if (!audioSource.enabled) //Pengecekan jika AudioSource nonaktif
            {
                audioSource.enabled = true; //Pengaktifan AudioSource
            }

            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            else
            {
                audioSource.clip = successClip;
                audioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("AudioSource atau AudioClip belum diatur!");
        }
    }
}
