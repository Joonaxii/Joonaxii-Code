namespace Joonaxii.Pooling
{
    public interface IPoolable
    {
        void OnCreate(ObjectPool pool);
    }
}