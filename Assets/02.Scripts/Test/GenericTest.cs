using UnityEngine;

// 제네릭(generic: 일반화)은 "타입(자료형)에 상관없는~"
// -> 이 자료형을 ㅏ용할 때 ㅈ료형이 무엇인지 정의하지 않고 그때그때 쓰는 것을
// -> Cup of Drink

public class Water
{ }
public class Cola
{ }

public class Cup<T>
{
    public T Drink;   // 명사

    public Cup(T drink)
    {
        Drink = drink;
    }
}

public class GenericTest : MonoBehaviour
{
    private void Start()
    {
        Cup<int> cup1 = new Cup<int>(17);
        Cup<float> cup2 = new Cup<float>(0.2f);
        Cup<string> cup3 = new Cup<string>("콜라");
        Cup<bool> cup4 = new Cup<bool>(true);
        Cup<Water> cup5 = new Cup<Water>(new Water());
        Cup<Cola> cup6 = new Cup<Cola>(new Cola());
    }
}
