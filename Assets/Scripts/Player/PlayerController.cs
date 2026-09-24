using UnityEngine;

namespace Justiceiro
{
    /// <summary>
    /// Movimento estilo DOOM 2016/Eternal: rápido, sem inércia pesada,
    /// pulo duplo, dash com cargas, controle total no ar.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Chão")]
        public float velocidade = 9.6f;
        public float velocidadeCorrida = 13.4f;
        public float aceleracao = 90f;
        public float atrito = 12f;

        [Header("Ar")]
        public float controleNoAr = 0.75f;
        public float gravidade = -28f;
        public float forcaPulo = 9.4f;
        public int pulosMax = 2;

        [Header("Dash")]
        public float dashVelocidade = 33f;
        public float dashDuracao = 0.16f;
        public float dashRecarga = 1.05f;
        public int dashCargasMax = 2;

        [Header("Mouse")]
        public float sensibilidade = 2.4f;
        public float limiteVertical = 89f;

        [Header("Câmera")]
        public Transform cam;
        public float alturaOlho = 1.62f;
        public float fovBase = 100f;
        public float fovCorrida = 108f;

        CharacterController cc;
        Vector3 velH;           // velocidade horizontal
        float velY;
        float pitch;
        int pulosRestantes;
        int dashCargas;
        float dashRecargaT;
        float dashT;
        Vector3 dashDir;
        float bobT;
        float tiltAlvo;
        float tiltAtual;

        public bool NoChao => cc != null && cc.isGrounded;
        public int DashCargas => dashCargas;
        public float DashRecargaNorm => Mathf.Clamp01(1f - dashRecargaT / dashRecarga);
        public Vector3 VelocidadeHorizontal => velH;
        public bool Dashando => dashT > 0f;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            dashCargas = dashCargasMax;
            pulosRestantes = pulosMax;
            if (cam != null)
            {
                var c = cam.GetComponent<Camera>();
                if (c != null) c.fieldOfView = fovBase;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            bool livre = gm == null || gm.estado == EstadoJogo.Jogando;

            if (livre) Olhar();
            Mover(livre);
            Camera_();
        }

        void Olhar()
        {
            if (WeaponController.RodaAbertaGlobal) return;
            float mx = Input.GetAxisRaw("Mouse X") * sensibilidade;
            float my = Input.GetAxisRaw("Mouse Y") * sensibilidade;

            transform.Rotate(0f, mx, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - my, -limiteVertical, limiteVertical);
        }

        void Mover(bool livre)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float ix = livre ? Input.GetAxisRaw("Horizontal") : 0f;
            float iz = livre ? Input.GetAxisRaw("Vertical") : 0f;
            Vector3 dirDesejada = (transform.right * ix + transform.forward * iz);
            if (dirDesejada.sqrMagnitude > 1f) dirDesejada.Normalize();

            bool correndo = livre && Input.GetKey(KeyCode.LeftShift);
            float vAlvo = correndo ? velocidadeCorrida : velocidade;

            // ---- dash
            if (dashRecargaT > 0f)
            {
                dashRecargaT -= dt;
                if (dashRecargaT <= 0f && dashCargas < dashCargasMax)
                {
                    dashCargas++;
                    if (dashCargas < dashCargasMax) dashRecargaT = dashRecarga;
                }
            }

            if (livre && Input.GetKeyDown(KeyCode.LeftControl) && dashCargas > 0 && dashT <= 0f)
            {
                dashCargas--;
                if (dashRecargaT <= 0f) dashRecargaT = dashRecarga;
                dashT = dashDuracao;
                dashDir = dirDesejada.sqrMagnitude > 0.01f ? dirDesejada.normalized : transform.forward;
                velY = Mathf.Max(velY, 0f);
                Sfx.Tocar("arma_troca", 0.35f, 1.6f);
            }

            if (dashT > 0f)
            {
                dashT -= dt;
                velH = dashDir * dashVelocidade;
                velY = Mathf.Max(velY, -1f);
            }
            else
            {
                Vector3 alvo = dirDesejada * vAlvo;
                float a = cc.isGrounded ? aceleracao : aceleracao * controleNoAr;
                velH = Vector3.MoveTowards(velH, alvo, a * dt);

                if (cc.isGrounded && dirDesejada.sqrMagnitude < 0.01f)
                    velH = Vector3.MoveTowards(velH, Vector3.zero, atrito * vAlvo * dt);
            }

            // ---- pulo
            if (cc.isGrounded && velY <= 0f)
            {
                velY = -2f;
                pulosRestantes = pulosMax;
            }

            if (livre && Input.GetKeyDown(KeyCode.Space) && pulosRestantes > 0)
            {
                pulosRestantes--;
                velY = forcaPulo;
                if (pulosRestantes < pulosMax - 1)
                {
                    // pulo duplo empurra na direção do input
                    if (dirDesejada.sqrMagnitude > 0.01f)
                        velH = Vector3.Lerp(velH, dirDesejada * vAlvo * 1.05f, 0.7f);
                    Sfx.Tocar("arma_troca", 0.3f, 1.9f);
                }
            }

            velY += gravidade * dt;
            velY = Mathf.Max(velY, -70f);

            Vector3 delta = (velH + Vector3.up * velY) * dt;
            cc.Move(delta);

            if (cc.isGrounded && velY < 0f) velY = -2f;

            // head bob
            float vel = Util.Plano(velH).magnitude;
            if (cc.isGrounded && vel > 1f) bobT += dt * vel * 1.1f;
            else bobT = Mathf.Lerp(bobT, 0f, dt * 6f);

            tiltAlvo = -ix * 1.9f + (dashT > 0f ? 0f : 0f);
        }

        void Camera_()
        {
            if (cam == null) return;
            float dt = Time.deltaTime;

            float vel = Util.Plano(velH).magnitude;
            float bobY = Mathf.Sin(bobT * 2f) * 0.045f * Mathf.Clamp01(vel / velocidade);
            float bobX = Mathf.Cos(bobT) * 0.035f * Mathf.Clamp01(vel / velocidade);

            tiltAtual = Mathf.Lerp(tiltAtual, tiltAlvo, dt * 9f);
            cam.localPosition = new Vector3(bobX, alturaOlho + bobY, 0f);
            cam.localRotation = Quaternion.Euler(pitch, 0f, tiltAtual);

            var c = cam.GetComponent<Camera>();
            if (c != null)
            {
                float alvoFov = fovBase + (vel / velocidadeCorrida) * (fovCorrida - fovBase);
                if (dashT > 0f) alvoFov += 8f;
                c.fieldOfView = Mathf.Lerp(c.fieldOfView, alvoFov, dt * 8f);
            }
        }

        public void Empurrar(Vector3 forca)
        {
            velH += Util.Plano(forca);
            velY += forca.y;
        }
    }
}
