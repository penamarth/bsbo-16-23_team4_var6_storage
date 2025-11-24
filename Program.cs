using System;
using System.Threading;


public interface IStorage
{
    bool CanStore();              // проверка, можно ли хранить
    string? GetEmpty(string path); // поиск пустого места
}

public abstract class StorageComposite : IStorage
{
    protected readonly List<IStorage> children = new();

    public virtual bool CanStore()
    {
        foreach (var child in children)
            if (child.CanStore())
                return true;
        return false;
    }

    public virtual string? GetEmpty(string path)
    {
        foreach (var child in children)
        {
            var result = child.GetEmpty(path);
            if (result != null)
                return result;
        }
        return null;
    }
}

public class StorageCell : IStorage
{
    private bool isOccupied;
    private readonly string id;

    public StorageCell(string id)
    {
        this.id = id;
        isOccupied = false;
    }

    public bool CanStore() => !isOccupied;

    public string? GetEmpty(string path)
    {
        Console.WriteLine($"Проверка ячейки {path}/{id} (занята: {isOccupied})");
        if (!isOccupied)
        {
            isOccupied = true;
            Console.WriteLine($"Выделена: {path}/{id}");
            return $"{path}/{id}";
        }
        return null;
    }
}
public class StorageShelf : StorageComposite
{
    private readonly string shelfId;

    public StorageShelf(string shelfId)
    {
        this.shelfId = shelfId;
    }

    public void AddCell(StorageCell cell) => children.Add(cell);

    public override string? GetEmpty(string path)
    {
        Console.WriteLine($"Входим на стеллаж {path}/{shelfId}");
        return base.GetEmpty($"{path}/{shelfId}");
    }
}
public class StorageRoom : StorageComposite
{
    private readonly string roomId;

    public StorageRoom(string roomId)
    {
        this.roomId = roomId;
    }

    public void AddShelf(StorageShelf shelf) => children.Add(shelf);

    public override string? GetEmpty(string path)
    {
        Console.WriteLine($"Входим в комнату {roomId}");
        return base.GetEmpty(roomId);
    }
}
public class StorageRoot : StorageComposite
{
    public StorageRoot(int roomsCount, int shelvesPerRoom, int cellsPerShelf)
    {
        for (int r = 1; r <= roomsCount; r++)
        {
            var room = new StorageRoom($"Room-{r}");

            for (int s = 1; s <= shelvesPerRoom; s++)
            {
                var shelf = new StorageShelf($"Shelf-{s}");

                for (int c = 1; c <= cellsPerShelf; c++)
                {
                    shelf.AddCell(new StorageCell($"Cell-{c}"));
                }

                room.AddShelf(shelf);
            }

            children.Add(room);
        }
    }
}
public interface iReposStor
{
    public bool SaveToPlacement(Dictionary<Item, string> data);
}
public class storRepos: iReposStor
{
    public storRepos()
    {
        Console.WriteLine("Подключение к бд  расположений... ");
        Thread.Sleep(2000);
    }
    public bool SaveToPlacement(Dictionary<Item, string> data) {
        Console.WriteLine($"Товары записаны в бд");
        return true;

    }
}

public interface iReposDoc
{
    public bool SaveDocument(string document);
}
public class docRepos : iReposDoc
{
    public docRepos()
    {
        Console.WriteLine("Подключение к бд документов... ");
        Thread.Sleep(2000);
    }
    public bool SaveDocument(string document)
    {
        Console.WriteLine($"Документ записан в бд");
        return true;

    }
}
public interface iDoc
{
    public string Create(string metad,Dictionary<Item, string> payload);
}
public class AcceptanceDoc : iDoc
{
    public string Create(string metad, Dictionary<Item, string> payload)
    {
        Console.WriteLine("Создаем документ");
        Thread.Sleep(1000);
        string document = $"Была произведена ПРИЕМКА товаров:\nМетаданные: {metad}\n";
        foreach (var item in payload)
        {
            string t_item = $"Item: {item.Key.Name}-{item.Key.Quantity}  adress: {item.Value}\n";
            document += t_item;
        }
        Console.WriteLine("Создан документ ПРИЕМКИ");
        Console.WriteLine(document);
        return document;
    }
}
public class DocWorks
{
    private iDoc doc;
    private iReposDoc docbd;
    public DocWorks(iDoc doc, iReposDoc docbd)
    {
        this.doc = doc;
        this.docbd = docbd;
    }
    public string createDocument(string metad, Dictionary<Item, string> payload)
    {
        string document = doc.Create(metad, payload);
        return document;
    } 
    public bool saveDocument(string document)
    {
        docbd.SaveDocument(document);
        return true;
    }
}

public interface iDataInputStrategy
{
    string GetData();
}

// === Реализации стратегий ===
public class ExternalSystemInput : iDataInputStrategy
{
    string data = "batch=123;date=2025-11-22;producer=Краснодарский_сельхоз;manager=Ivanov;items=мандарины:10кг,апельсины:3кг,яблоки:5кг\r\n";
    public string GetData()
    {
        // Заглушка для обращения к внешней системе
        Console.WriteLine("Получение данных из внешней системы...");
        return data;
    }
}

public class QRCodeInput : iDataInputStrategy
{
    private string data = "batch=123;date=2025-11-22;producer=Краснодарский_сельхоз;items=мандарины:10кг,апельсины:3кг,яблоки:5кг\r\n";
    public string GetData()
    {
        // Заглушка для сканирования QR
        Console.WriteLine("Сканирование QR-кода...");
        return data;
    }
}

public class ManualInput : iDataInputStrategy
{
    public string GetData()
    {
        Console.WriteLine("Введите данные вручную:");
        return Console.ReadLine();
    }
}
public interface iProcess
{
    void Parse(string data);   // парсинг строки
    Dictionary<Item, string> ExecuteStorageLogic(IStorage house);      // выполнение логики
}
public class Item
{
    public string Name { get; set; }
    public string Quantity { get; set; }
}


public class Acceptance : iProcess
{
    private string batchId;
    private string date;
    private string producer;
    private string manager;

    private List<Item> items = new List<Item>();
    public Dictionary<Item, string> payload = new Dictionary<Item, string>();

    public void Parse(string data)
    {
        var parts = data.Split(';');
        var dict = new Dictionary<string, string>();

        foreach (var part in parts)
        {
            var kv = part.Split('=');
            if (kv.Length == 2)
                dict[kv[0]] = kv[1];
        }

        batchId = dict.ContainsKey("batch") ? dict["batch"] : "не указан";
        date = dict.ContainsKey("date") ? dict["date"] : "не указана";
        producer = dict.ContainsKey("producer") ? dict["producer"] : "не указан";
        manager = dict.ContainsKey("manager") ? dict["manager"] : "не указан";

        if (dict.ContainsKey("items"))
        {
            foreach (var item in dict["items"].Split(','))
            {
                var kv = item.Split(':');
                if (kv.Length == 2)
                {
                    items.Add(new Item
                    {
                        Name = kv[0].Trim(),
                        Quantity = kv[1].Trim()
                    });
                }
            }
        }
    }

    public Dictionary<Item,string> ExecuteStorageLogic(IStorage house)
    {
        Console.WriteLine($"Приемка поставки №{batchId}");
        Console.WriteLine($"Дата: {date}");
        Console.WriteLine($"Производитель: {producer}");
        Console.WriteLine($"Менеджер: {manager}");
        Console.WriteLine("Список товаров:");
        foreach (var item in items)
        {
            Console.WriteLine($"- {item.Name}, {item.Quantity}");
            // Поиск свободной ячейки
            string? cellAddress = house.GetEmpty("Warehouse");

            if (cellAddress != null)
            {
                this.payload.Add(item, cellAddress);
                Console.WriteLine($"Размещено в: {cellAddress}");
            }
            else
            {
                Console.WriteLine("Нет свободных ячеек для размещения");
            }
        }
        return payload;
    }
}


public class StorageExecutor
{
    private IStorage house;
    private iReposStor storbd;
    public StorageExecutor(IStorage house, iReposStor storbd)
    {
        this.house = house;
        this.storbd = storbd;
    }
    public Dictionary<Item, string> ExecuteProcess(iProcess process, string data)
    {
        process.Parse(data);   // парсинг строки
        var payload = process.ExecuteStorageLogic(house);     // выполнение логики
        storbd.SaveToPlacement(payload);
        return payload;

    }
}


public class StorageHouse
{
     private string type;
     private iDataInputStrategy inputStrategy;
     private iProcess process;
     private StorageRoot root;
     private StorageExecutor storageExecutor;
     private DocWorks docWorks;
     public StorageHouse()
     {
        int rooms = 2, shelves = 1, cells = 2;
        this.root = new StorageRoot(rooms, shelves, cells);
        var storbd = new storRepos();
        this.storageExecutor = new StorageExecutor(root, storbd);
        var docbd = new docRepos();
        var doc = new AcceptanceDoc();
        this.docWorks = new DocWorks(doc, docbd);
        Console.WriteLine("Вы инициализировали систему склада ...");
        Thread.Sleep(1000);

     }
    void getProcessType()
    {
        Console.WriteLine("Выберите тип процесса. 1-приемка, 2-заказ");
        this.type = Console.ReadLine();
        if (this.type == "1")
        {
            process = new Acceptance();

        }
        this.process = process;
    }
    void getInputStrategy()
    {
        Console.WriteLine("""
            Выберете как вы хотите подать данные 
            1 - внешняя система,
            2 - QR,
            3 - ввести документ вручную");
            """);

        string choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                inputStrategy = new ExternalSystemInput();
                break;
            case "2":
                inputStrategy = new QRCodeInput();
                break;
            case "3":
                inputStrategy = new ManualInput();
                break;
            default:
                Console.WriteLine("Неверный выбор, используем ввод вручную.");
                inputStrategy = new ManualInput();
                break;
        }
        this.inputStrategy = inputStrategy;
    }
     string getMetaData()
     {
        Console.WriteLine("Введите текущую дату");
        string data = Console.ReadLine() + ";";
        Console.WriteLine("Введите свою фамилию");
        data += Console.ReadLine();
        return data;
     }
    public void ExecuteProcess()
    {

        getProcessType();
        getInputStrategy();
        // Используем выбранную стратегию
        string data = inputStrategy.GetData();
        Thread.Sleep(2000);
        Console.WriteLine($"Получены данные: {data}");
        Console.WriteLine("Передаем на управление процессами ...");
        Thread.Sleep(1000);
        var payload = storageExecutor.ExecuteProcess(process, data);

        Console.WriteLine("Передаем на создание соответствующего документа ...");
        string metad = getMetaData();
        string document = docWorks.createDocument(metad, payload);
        bool res = docWorks.saveDocument(document);
        if (res) {
            Console.WriteLine("Завершена работа с процессом!");
        }
    }

}
class Program
{
    
    static void Main()
    {
        var program = new StorageHouse();
        program.ExecuteProcess();

        
    }
}

