namespace AliceHat.Models.Alice.Abstract
{
    public interface ICloneable<out T>
    {
        T Clone();
    }
}