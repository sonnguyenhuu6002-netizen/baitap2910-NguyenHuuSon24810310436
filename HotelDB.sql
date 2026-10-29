-- Chạy toàn bộ script này trong SSMS 20 (New Query -> Execute)
USE master;
GO
IF DB_ID('HotelDB') IS NULL CREATE DATABASE HotelDB;
GO
USE HotelDB;
GO
CREATE TABLE Phong (
    SoPhong     VARCHAR(10)   PRIMARY KEY,
    LoaiPhong   NVARCHAR(50)  NOT NULL,
    GiaTheoGio  DECIMAL(18,2) NOT NULL,
    TrangThai   NVARCHAR(30)  NOT NULL DEFAULT N'Trống'
        CHECK (TrangThai IN (N'Trống', N'Đang ở', N'Đang dọn dẹp'))
);
CREATE TABLE KhachHang (
    MaKH        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    CCCD        VARCHAR(20)   NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15)
);
CREATE TABLE DichVu (
    MaDV   INT IDENTITY(1,1) PRIMARY KEY,
    TenDV  NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL
);
CREATE TABLE PhieuDatPhong (
    MaPhieu           INT IDENTITY(1,1) PRIMARY KEY,
    SoPhong           VARCHAR(10) NOT NULL REFERENCES Phong(SoPhong),
    MaKH              INT NOT NULL REFERENCES KhachHang(MaKH),
    NgayNhanPhong     DATETIME NOT NULL,
    NgayTraPhong      DATETIME NULL,
    TongTienThanhToan DECIMAL(18,2) NULL
);
CREATE TABLE SuDungDichVu (
    MaSD     INT IDENTITY(1,1) PRIMARY KEY,  -- khóa thay thế: cho phép gọi cùng 1 dịch vụ nhiều lần
    MaPhieu  INT NOT NULL REFERENCES PhieuDatPhong(MaPhieu),
    MaDV     INT NOT NULL REFERENCES DichVu(MaDV),
    SoLuong  INT NOT NULL CHECK (SoLuong > 0),
    ThoiGian DATETIME NOT NULL DEFAULT GETDATE()
);
GO
INSERT INTO Phong VALUES
 ('P101',N'Đơn',80000,N'Trống'),('P102',N'Đơn',80000,N'Trống'),
 ('P201',N'Đôi',120000,N'Trống'),('P202',N'Đôi',120000,N'Trống'),
 ('P301',N'VIP',250000,N'Trống'),('P302',N'VIP',250000,N'Đang dọn dẹp');
INSERT INTO DichVu(TenDV,DonGia) VALUES
 (N'Nước suối',15000),(N'Bia lon',30000),(N'Giặt là',50000),(N'Mì ly',25000),(N'Cơm phần',60000);
GO
