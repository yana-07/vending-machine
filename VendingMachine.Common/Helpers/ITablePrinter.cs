namespace VendingMachine.Common.Helpers;

public interface ITablePrinter<T>
{
    void Print(IEnumerable<T> items);
}
