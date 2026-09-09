using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena
{
    public class Potion : Projectile
    {
        protected override void OnImpact()
        {
            var body = transform.Find("Sphere");
            if (body == null) return;
            var mesh = body.GetComponent<MeshFilter>();
            var source = body.GetComponent<MeshRenderer>();
            if (mesh == null || source == null) return;

            var splash = new GameObject("Potion splash");
            splash.transform.position = transform.position;
            var particles = splash.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 14;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh.sharedMesh;
            renderer.sharedMaterial = source.sharedMaterial;
            particles.Play();
            particles.Emit(14);
        }
    }
}
