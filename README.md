# 使用向导

数据库使用的sqlver，可以用其他连接字符在 webconfig.xml 里面修改

数据库生成器：

<details>
  <summary>查看代码</summary>

```sql
create database shop collate Chinese_PRC_CI_AS
go

grant connect on database :: shop to dbo
go

grant view any column encryption key definition, view any column master key definition on database :: shop to [public]
go

create table dbo.Admin
(
    id        int identity
        constraint id
            primary key,
    account   varchar(20)  not null
        constraint Admin_pk
            unique,
    pwd       varchar(100) not null,
    authority int          not null
        constraint [authority-check]
            check ([authority] = 3 OR [authority] = 2 OR [authority] = 1)
)
go

CREATE TRIGGER trg_CheckAccountForAuthority3
    ON Admin
    AFTER INSERT, UPDATE
    AS
BEGIN
    -- 检查 authority = 3 的情况下，account 是否存在于 PlayerList 表中
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.authority = 3
          AND NOT EXISTS (SELECT 1 FROM PlayerList p WHERE p.account = i.account)
    )
        BEGIN
            RAISERROR ('The account for authority level 3 must exist in the PlayerList table.', 16, 1);
            ROLLBACK TRANSACTION;
        END
END
go

create table dbo.PlayerList
(
    id              int identity
        constraint PlayerList_Id_pak
            primary key,
    name            varchar(50) not null
        constraint PlayerList_name_pk
            unique,
    gender          varchar(50) not null
        constraint gender_checker
            check ([gender] = 'null' OR [gender] = '女' OR [gender] = '男'),
    account         varchar(50)
        constraint PlayerList_account_pk
            unique,
    pwd             varchar(100),
    DeliveryAddress varchar(50),
    email           varchar(20),
    phone           varchar(20),
    birthday        datetime
        constraint DF_PlayerList_Birthday default getdate(),
    hobbies         varchar(20),
    income          int          default 0,
    avatar          varchar(100) default 'avatar'
)
go

create table dbo.Orders
(
    ID              int identity
        constraint Orders_Key_pk
            primary key,
    belong          varchar(50)                        not null
        constraint Orders_PlayerList_account_fk
            references dbo.PlayerList (account),
    baby            varchar(8000)                      not null,
    time            datetime                           not null,
    status          bit                                not null,
    orderNumber     uniqueidentifier default newid(),
    logistics       varchar(50)      default N'未发货' not null,
    refund          varchar(15)      default '未开启'  not null
        constraint refund_check
            check ([refund] = '失败' OR [refund] = '成功' OR [refund] = '待处理' OR [refund] = '未开启'),
    handling_number int              default 0
        constraint handing_number_check
            check ([handling_number] <= 4)
)
go

exec sp_addextendedproperty 'MS_Description', N'订单', 'SCHEMA', 'dbo', 'TABLE', 'Orders'
go

exec sp_addextendedproperty 'MS_Description', 'Key', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'ID'
go

exec sp_addextendedproperty 'MS_Description', N'用户', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'belong'
go

exec sp_addextendedproperty 'MS_Description', N'商品集合', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'baby'
go

exec sp_addextendedproperty 'MS_Description', N'时间', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'time'
go

exec sp_addextendedproperty 'MS_Description', N'订单状态', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'status'
go

exec sp_addextendedproperty 'MS_Description', N'物流', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'logistics'
go

exec sp_addextendedproperty 'MS_Description', N'退款', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN', 'refund'
go

exec sp_addextendedproperty 'MS_Description', N'操作次数', 'SCHEMA', 'dbo', 'TABLE', 'Orders', 'COLUMN',
     'handling_number'
go

-- Orders 下单速度
CREATE TRIGGER trg_Orders_CheckTime
    ON Orders
    INSTEAD OF INSERT
    AS
BEGIN
    -- 检查 Orders 表中是否已经有记录
    IF EXISTS (SELECT 1 FROM Orders)
        BEGIN
            -- 处理批量插入的情况
            DECLARE @newBelong varchar(50), @newTime datetime;

            -- 检查是否有冲突的 belong 和时间小于 60 秒的情况
            IF EXISTS (
                SELECT 1
                FROM inserted i
                         JOIN Orders o ON i.belong = o.belong
                WHERE DATEDIFF(SECOND, o.time, i.time) < 60
            )
                BEGIN
                    -- 抛出异常，阻止插入
                    RAISERROR('插入失败: 相同用户的订单间隔小于 60 秒', 16, 1);
                    ROLLBACK TRANSACTION;
                    RETURN;
                END
        END

    -- 如果没有订单数据或没有冲突的记录，插入新记录
    INSERT INTO Orders (orderNumber, belong, baby, time, status)
    SELECT orderNumber, belong, baby, time, status FROM inserted;
END
go

create table dbo.babys
(
    id         int identity
        constraint babys_pk
            primary key,
    name       varchar(50)                  not null
        constraint babys_pk_2
            unique,
    city       varchar(30)                  not null,
    time       date                         not null,
    address    varchar(100)                 not null,
    photo      varchar(100)                 not null,
    price      int                          not null,
    type       varchar(10)                  not null,
    total      int                          not null,
    alreadyBuy int,
    belongs    varchar(20)                  not null
        constraint belongs
            references dbo.Admin (account),
    brand      varchar(20) default '个人所有',
    message    varchar(50) default '商家很懒暂未添加留言',
    attribute  varchar(10) default 'really' not null
        constraint [attribute-check]
            check ([babys].[attribute] = 'really' OR [babys].[attribute] = 'virtual'),
    constraint alreadyBuy_check
        check ([alreadyBuy] <= [babys].[total])
)
go

create table dbo.DetailsPhoto
(
    id         int identity
        constraint DetailsPhoto_pk
            primary key,
    Photo      varchar(100)                 not null,
    Baby       int                          not null
        constraint DetailsPhoto_babys_id_fk
            references dbo.babys,
    Types      varchar(10) default '默认'   not null,
    position   varchar(10) default 'banner' not null
        constraint position_check
            check ([DetailsPhoto].[position] = 'show' OR [DetailsPhoto].[position] = 'banner' OR
                   [DetailsPhoto].[position] = 'total'),
    Price      int         default 0,
    total      int         default 0,
    alreadyBuy int         default 0,
    belong     varchar(20)                  not null
        constraint DetailsPhoto___belong
            references dbo.Admin (account)
)
go

CREATE TRIGGER trg_CheckDetailsPhotoTotal
    ON DetailsPhoto
    AFTER INSERT, UPDATE
    AS
BEGIN
    -- 定义变量来存储插入或更新的 Baby ID、alreadyBuy、total、和总和
    DECLARE @BabyId INT, @TotalSum INT, @BabyTotal INT, @AlreadyBuy INT, @RowTotal INT;

    -- 获取插入或更新的 Baby ID
    SELECT @BabyId = i.Baby, @AlreadyBuy = i.alreadyBuy, @RowTotal = i.total
    FROM inserted i;

    -- 检查 alreadyBuy 是否大于 total（对于当前插入或更新的行）
    IF (@AlreadyBuy > @RowTotal)
        BEGIN
            -- 如果 alreadyBuy 超过 total，则触发错误并回滚事务
            RAISERROR ('DetailsPhoto 中的 alreadyBuy 不能超过 total.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

    -- 计算 DetailsPhoto 中所有 Baby 相同且 Types = 'total' 的行的 total 值总和
    SELECT @TotalSum = SUM(d.total)
    FROM DetailsPhoto d
    WHERE d.Baby = @BabyId AND d.position = 'total';

    -- 获取 babys 表中对应 Baby 的 total 值
    SELECT @BabyTotal = b.total
    FROM babys b
    WHERE b.id = @BabyId;

    -- 检查 DetailsPhoto 中的 total 总和是否超过 babys 表中的 total
    IF (@TotalSum > @BabyTotal)
        BEGIN
            -- 如果超过了，则触发错误并回滚事务
            RAISERROR ('DetailsPhoto 中的 total 总和不能超过 babys 表中的 total.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
END;
go

create table dbo.shipped
(
    id           int identity
        constraint shipped_pk
            primary key,
    shop         varchar(20)                     not null
        constraint shipped_Admin_account_fk
            references dbo.Admin (account),
    baby         int                             not null
        constraint shipped_babys_id_fk
            references dbo.babys,
    logistics    varchar(50) default N'暂未发货' not null,
    [bind-order] int                             not null
        constraint shipped_Orders_ID_fk
            references dbo.Orders
)
go

create table dbo.shoppingCart
(
    account varchar(50)                   not null
        constraint [User-bind]
            references dbo.PlayerList (account),
    item    int                           not null
        constraint [item-bind]
            references dbo.babys,
    count   int
        constraint [default] default 0    not null,
    type    varchar(10) default 'default' not null,
    id      int identity
        constraint shoppingCart_pk
            primary key
)
go

CREATE TRIGGER CheckAndUpdateItemCountBeta
    ON shoppingCart
    AFTER INSERT, UPDATE
    AS
BEGIN
    DECLARE @Type VARCHAR(10);            -- 声明类型变量
    DECLARE @Count INT;                   -- 声明数量变量
    DECLARE @BabyId INT;                  -- 声明商品ID变量
    DECLARE @AvailableCount INT;          -- 声明可用数量变量
    DECLARE @CurrentAlreadyBuy INT;       -- 声明当前已购买数量变量

    -- 从插入的行中获取值
    SELECT @Type = i.type, @Count = i.count, @BabyId = i.item
    FROM inserted i;

    -- 判断是否为更新操作
    IF EXISTS (SELECT 1 FROM deleted WHERE item = @BabyId)
        BEGIN
            DECLARE @OldCount INT;

            -- 获取修改前的数量
            SELECT @OldCount = d.count
            FROM deleted d
            WHERE d.item = @BabyId;

            IF @Type = 'default'
                BEGIN
                    -- 更新类型为 'default' 的逻辑
                    UPDATE b
                    SET b.alreadyBuy = b.alreadyBuy - @OldCount + @Count
                    FROM babys b
                    WHERE b.id = @BabyId;

                    -- 检查是否超过总库存
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                                 JOIN babys b ON i.item = b.id
                        WHERE i.count > b.total
                    )
                        BEGIN
                            RAISERROR ('商品数量超过 babys 表中的可用总数。', 16, 1);
                            ROLLBACK TRANSACTION;          -- 回滚事务
                            RETURN;                       -- 返回
                        END
                END
            ELSE
                BEGIN
                    -- 处理类型不为 'default' 的逻辑
                    -- 检查类型是否在 DetailsPhoto 表中存在
                    IF NOT EXISTS (
                        SELECT 1
                        FROM DetailsPhoto
                        WHERE Types = @Type AND Baby = @BabyId
                    )
                        BEGIN
                            RAISERROR (N'无效的款式指定。该款式在 DetailsPhoto 中不存在。', 16, 1);
                            ROLLBACK TRANSACTION;              -- 回滚事务
                            RETURN;                           -- 返回
                        END

                    -- 获取可用数量
                    SELECT @AvailableCount = d.total - ISNULL(b.alreadyBuy, 0)
                    FROM DetailsPhoto d
                             JOIN babys b ON b.id = @BabyId
                    WHERE d.Baby = @BabyId AND d.position = 'total';

                    -- 检查数量是否超过可用数量
                    IF (@Count > @AvailableCount)
                        BEGIN
                            RAISERROR ('指定类型的可用数量不足。', 16, 1);
                            ROLLBACK TRANSACTION;          -- 回滚事务
                            RETURN;                       -- 返回
                        END

                    -- 更新 DetailsPhoto 表中的已购买数量
                    UPDATE d
                    SET d.alreadyBuy = d.alreadyBuy - @OldCount + @Count
                    FROM DetailsPhoto d
                    WHERE d.Baby = @BabyId AND d.position = 'total';
                END
        END
    ELSE
        BEGIN
            -- 处理插入操作
            IF @Type = 'default'
                BEGIN
                    -- 检查总库存
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                                 JOIN babys b ON i.item = b.id
                        WHERE i.count > b.total
                    )
                        BEGIN
                            RAISERROR ('商品数量超过 babys 表中的可用总数。', 16, 1);
                            ROLLBACK TRANSACTION;          -- 回滚事务
                            RETURN;                       -- 返回
                        END

                    -- 更新 babys 表中的已购买数量
                    UPDATE b
                    SET b.alreadyBuy = b.alreadyBuy + @Count
                    FROM babys b
                    WHERE b.id = @BabyId;
                END
            ELSE
                BEGIN
                    -- 检查类型是否在 DetailsPhoto 表中存在
                    IF NOT EXISTS (
                        SELECT 1
                        FROM DetailsPhoto
                        WHERE Types = @Type AND Baby = @BabyId
                    )
                        BEGIN
                            RAISERROR (N'无效的款式指定。该款式在 DetailsPhoto 中不存在。', 16, 1);
                            ROLLBACK TRANSACTION;              -- 回滚事务
                            RETURN;                           -- 返回
                        END

                    -- 获取可用数量
                    SELECT @AvailableCount = d.total - ISNULL(b.alreadyBuy, 0)
                    FROM DetailsPhoto d
                             JOIN babys b ON b.id = @BabyId
                    WHERE d.Baby = @BabyId AND d.position = 'total';

                    -- 检查数量是否超过可用数量
                    IF (@Count > @AvailableCount)
                        BEGIN
                            RAISERROR ('指定类型的可用数量不足。', 16, 1);
                            ROLLBACK TRANSACTION;          -- 回滚事务
                            RETURN;                       -- 返回
                        END

                    -- 更新 DetailsPhoto 表中的已购买数量
                    UPDATE d
                    SET d.alreadyBuy = d.alreadyBuy + @Count
                    FROM DetailsPhoto d
                    WHERE d.Baby = @BabyId AND d.position = 'total';
                END
        END
END;
go
```
</details>
