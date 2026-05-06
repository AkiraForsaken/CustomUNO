using UnityEngine;

// Script này tự động yêu cầu Unity gắn thêm AudioSource vào GameObject
[RequireComponent(typeof(AudioSource))]
public class GameFeedbackManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip drawCardSound;
    [SerializeField] private AudioClip playCardSound;

    [Header("Visual Effects (VFX)")]
    [Tooltip("Kéo Particle System hoặc hiệu ứng nổ UI vào đây")]
    [SerializeField] private ParticleSystem playCardVFX;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Không cho âm thanh tự động phát khi mới vào game
        audioSource.playOnAwake = false; 
    }

    private void OnEnable()
    {
        // Lắng nghe sự kiện từ GameEvents
        GameEvents.OnDrawCardRequested += PlayDrawFeedback;
        GameEvents.OnPlayCardRequested += PlayCardFeedback;
    }

    private void OnDisable()
    {
        // Hủy lắng nghe khi object bị tắt
        GameEvents.OnDrawCardRequested -= PlayDrawFeedback;
        GameEvents.OnPlayCardRequested -= PlayCardFeedback;
    }

    private void PlayDrawFeedback()
    {
        // Phát tiếng rút bài
        if (drawCardSound != null)
        {
            audioSource.PlayOneShot(drawCardSound);
        }
    }

    private void PlayCardFeedback(CardInstance card)
    {
        // Phát tiếng đánh bài
        if (playCardSound != null)
        {
            // Có thể chỉnh cao độ (pitch) ngẫu nhiên một chút nghe cho vui tai
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(playCardSound);
        }

        // Bắn hiệu ứng hình ảnh (Particle System)
        if (playCardVFX != null)
        {
            playCardVFX.Play();
        }
    }
}