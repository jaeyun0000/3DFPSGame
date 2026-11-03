using System.Collections.Generic;
using UnityEngine;

// 자주 생기는 파티클 이펙트(총알 자국, 폭발)를 프리팹마다 정해진 개수만 만들어 두고 돌려쓰는 풀
// - 이펙트는 재생이 끝나면 저절로 멈추므로 반납하지 않고, 가장 오래전에 쓴 것부터 다시 꺼내 쓴다. (링 버퍼)
// - Instantiate/Destroy를 반복하지 않으므로 생성 비용과 GC가 없고, 오브젝트 수도 개수 이상 늘지 않는다.
public static class EffectPool
{
    private const int DefaultSize = 20;

    private class Ring
    {
        public ParticleSystem[] Effects;
        public int Next;
    }

    private static readonly Dictionary<ParticleSystem, Ring> Rings = new Dictionary<ParticleSystem, Ring>();

    // 프리팹의 이펙트를 지정한 위치와 방향에서 처음부터 재생한다.
    // size: 동시에 보일 수 있는 최대 개수 (처음 꺼낼 때만 적용된다.)
    public static ParticleSystem Play(ParticleSystem prefab, Vector3 position, Quaternion rotation, int size = DefaultSize)
    {
        if (!Rings.TryGetValue(prefab, out Ring ring))
        {
            ring = new Ring { Effects = new ParticleSystem[size] };
            Rings.Add(prefab, ring);
        }

        // 아직 만들지 않았거나 씬이 다시 로드되어 파괴된 칸은 새로 만든다.
        ParticleSystem effect = ring.Effects[ring.Next];
        if (effect == null)
        {
            effect = Object.Instantiate(prefab);
            ring.Effects[ring.Next] = effect;

            // 프리팹은 재생이 끝나면 스스로 파괴되도록(Stop Action: Destroy) 되어 있어서, 풀에 넣을 복제본만 끄지 않도록 바꾼다.
            ParticleSystem.MainModule main = effect.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        ring.Next = (ring.Next + 1) % ring.Effects.Length;

        // 다른 곳에서 바꾼 크기가 남아 있지 않도록 프리팹 크기로 되돌린다.
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.transform.localScale = prefab.transform.localScale;

        // 재생 중인 이펙트는 Play만 하면 이어서 재생되므로, 지우고 처음부터 다시 재생한다.
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        return effect;
    }
}
