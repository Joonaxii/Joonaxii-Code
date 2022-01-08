namespace Joonaxii.Pooling
{
    public interface IFixedPoolable
    {
        void OnCreate(FixedObjectPool pool);
        void OnPoolableReused();
    }
}