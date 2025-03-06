using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{
    class EffectsComp : SubpartComponent
    {

        private static Dictionary<string, MySoundPair> cache = new Dictionary<string, MySoundPair>();
        private MyEntity3DSoundEmitter soundEmitter;

        public override void Close()
        {
            soundEmitter?.StopSound(true, true);
        }

        public void PlaySound(string sound, float volume = 1.0f)
        {
            if (soundEmitter == null)
            {
                soundEmitter = new MyEntity3DSoundEmitter(Subpart.MyPart);
            }

            if (!cache.ContainsKey(sound))
            {
                cache.Add(sound, new MySoundPair(sound));
            }

            if (soundEmitter != null)
            {
                soundEmitter.VolumeMultiplier = volume; // Set the volume
                soundEmitter.PlaySound(cache[sound]);
            }
        }

        private MyParticleEffect Create(string particle)
        {
            var matrix = Subpart.MyPart.WorldMatrix;
            var pos = Subpart.MyPart.WorldMatrix.Translation;
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(particle, ref matrix, ref pos, Subpart.MyPart.Render.GetRenderObjectID(), out effect))
            {
                return effect;
            }
            return null;
        }

        public void SpawnParticle(ParticleAnimation particle)
        {
            var p = Create(particle.Name);
            p.Autodelete = particle.AutoDelete;
            p.UserScale = particle.Scale;
            p.Autodelete = true;
            p.UserLifeMultiplier = particle.LifeMultiplier;

            if (particle.Velocity.HasValue)
                p.Velocity = particle.Velocity.Value;

            if (particle.Color.HasValue)
                p.UserColorMultiplier = particle.Color.Value;

            p.Play();
        }


    }
}
