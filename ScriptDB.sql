\xEF\xBB\xBF-- =============================================
-- ScriptDB.sql - CafePOSDB (Bai 5.3: POS Cafe)
-- Co the chay lai nhieu lan (se xoa bang cu va tao lai du lieu mau)
-- =============================================
IF DB_ID('CafePOSDB') IS NULL CREATE DATABASE CafePOSDB;
GO
USE CafePOSDB;
GO

IF OBJECT_ID('ChiTietHoaDon') IS NOT NULL DROP TABLE ChiTietHoaDon;
IF OBJECT_ID('HoaDon')        IS NOT NULL DROP TABLE HoaDon;
IF OBJECT_ID('MonAn')         IS NOT NULL DROP TABLE MonAn;
IF OBJECT_ID('DanhMuc')       IS NOT NULL DROP TABLE DanhMuc;
IF OBJECT_ID('BanAn')         IS NOT NULL DROP TABLE BanAn;
GO

CREATE TABLE BanAn (
    MaBan     INT IDENTITY(1,1) PRIMARY KEY,
    TenBan    NVARCHAR(50)  NOT NULL,
    TrangThai NVARCHAR(20)  NOT NULL DEFAULT N'Trống'
        CONSTRAINT CK_BanAn_TrangThai CHECK (TrangThai IN (N'Trống', N'Có khách'))
);

CREATE TABLE DanhMuc (
    MaDanhMuc  INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL
);

CREATE TABLE MonAn (
    MaMon     INT IDENTITY(1,1) PRIMARY KEY,
    TenMon    NVARCHAR(100) NOT NULL,
    MaDanhMuc INT NOT NULL CONSTRAINT FK_MonAn_DanhMuc REFERENCES DanhMuc(MaDanhMuc),
    DonGia    DECIMAL(18,2) NOT NULL CONSTRAINT CK_MonAn_DonGia CHECK (DonGia >= 0)
);

CREATE TABLE HoaDon (
    MaHoaDon  INT IDENTITY(1,1) PRIMARY KEY,
    MaBan     INT NOT NULL CONSTRAINT FK_HoaDon_Ban REFERENCES BanAn(MaBan),
    GioVao    DATETIME NOT NULL DEFAULT GETDATE(),
    GioRa     DATETIME NULL,
    TongTien  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TrangThai BIT NOT NULL DEFAULT 0      -- 0: chua thanh toan, 1: da thanh toan
);

CREATE TABLE ChiTietHoaDon (
    MaHoaDon INT NOT NULL CONSTRAINT FK_CT_HoaDon REFERENCES HoaDon(MaHoaDon),
    MaMon    INT NOT NULL CONSTRAINT FK_CT_MonAn  REFERENCES MonAn(MaMon),
    SoLuong  INT NOT NULL CONSTRAINT CK_CT_SoLuong CHECK (SoLuong > 0),
    DonGia   DECIMAL(18,2) NOT NULL,      -- gia tai thoi diem goi mon
    CONSTRAINT PK_ChiTietHoaDon PRIMARY KEY (MaHoaDon, MaMon)
);
GO

-- ===== Du lieu mau =====
INSERT BanAn(TenBan) VALUES (N'Bàn 01'),(N'Bàn 02'),(N'Bàn 03'),(N'Bàn 04'),(N'Bàn 05'),
                            (N'Bàn 06'),(N'Bàn 07'),(N'Bàn 08'),(N'Bàn 09'),(N'Bàn 10');
INSERT DanhMuc(TenDanhMuc) VALUES (N'Cà phê'),(N'Trà sữa'),(N'Đồ ăn nhẹ');
INSERT MonAn(TenMon, MaDanhMuc, DonGia) VALUES
 (N'Cà phê đen',1,20000),(N'Cà phê sữa',1,25000),(N'Bạc xỉu',1,28000),
 (N'Trà sữa trân châu',2,35000),(N'Trà sữa matcha',2,38000),
 (N'Bánh tiramisu',3,45000),(N'Khoai tây chiên',3,30000);

-- 2 hoa don da thanh toan de man hinh Thong ke co du lieu
INSERT HoaDon(MaBan, GioVao, GioRa, TongTien, TrangThai) VALUES
 (1, DATEADD(DAY,-2,GETDATE()), DATEADD(DAY,-2,DATEADD(MINUTE,50,GETDATE())), 70000, 1),
 (2, DATEADD(DAY,-1,GETDATE()), DATEADD(DAY,-1,DATEADD(MINUTE,70,GETDATE())), 105000, 1);
INSERT ChiTietHoaDon(MaHoaDon, MaMon, SoLuong, DonGia) VALUES
 (1,1,2,20000),(1,7,1,30000),
 (2,4,1,35000),(2,6,1,45000),(2,2,1,25000);
GO
