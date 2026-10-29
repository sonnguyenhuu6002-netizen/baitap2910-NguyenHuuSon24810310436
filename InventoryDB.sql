-- Chạy toàn bộ script trong SSMS 20. Script tự xóa và tạo lại InventoryDB nên chạy lại bao nhiêu lần cũng được.
USE master;
GO
IF DB_ID('InventoryDB') IS NOT NULL
BEGIN
    ALTER DATABASE InventoryDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE InventoryDB;
END
GO
CREATE DATABASE InventoryDB;
GO
USE InventoryDB;
GO
CREATE TABLE NhaCungCap (
    MaNCC      INT IDENTITY(1,1) PRIMARY KEY,
    TenNCC     NVARCHAR(150) NOT NULL,
    DienThoai  VARCHAR(15) NULL,
    DiaChi     NVARCHAR(255) NULL
);
-- UNIQUE cho DienThoai nhưng cho phép nhiều NCC chưa có số (UNIQUE thường chỉ cho 1 giá trị NULL)
CREATE UNIQUE INDEX UX_NhaCungCap_DienThoai ON NhaCungCap(DienThoai) WHERE DienThoai IS NOT NULL;

CREATE TABLE VatTu (
    MaVatTu     VARCHAR(20)   PRIMARY KEY,
    TenVatTu    NVARCHAR(150) NOT NULL,
    DVT         NVARCHAR(20)  NULL,
    SoLuongTon  DECIMAL(18,2) NOT NULL DEFAULT 0 CHECK (SoLuongTon >= 0)
);

CREATE TABLE PhieuKho (
    MaPhieu   INT IDENTITY(1,1) PRIMARY KEY,
    LoaiPhieu NVARCHAR(10) NOT NULL CHECK (LoaiPhieu IN (N'NHAP', N'XUAT')),
    NgayLap   DATETIME NOT NULL DEFAULT GETDATE(),
    MaNCC     INT NULL REFERENCES NhaCungCap(MaNCC),
    TongTien  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CHECK ((LoaiPhieu = N'NHAP' AND MaNCC IS NOT NULL) OR LoaiPhieu = N'XUAT')
);

CREATE TABLE ChiTietPhieuKho (
    MaPhieu  INT         NOT NULL REFERENCES PhieuKho(MaPhieu),
    MaVatTu  VARCHAR(20) NOT NULL REFERENCES VatTu(MaVatTu),
    SoLuong  DECIMAL(18,2) NOT NULL CHECK (SoLuong > 0),
    DonGia   DECIMAL(18,2) NOT NULL DEFAULT 0,
    PRIMARY KEY (MaPhieu, MaVatTu)
);
GO
-- Dữ liệu mẫu
INSERT INTO NhaCungCap(TenNCC, DienThoai, DiaChi) VALUES
 (N'Công ty Xi măng Hà Tiên', '0901000001', N'TP.HCM'),
 (N'Thép Hòa Phát',           '0901000002', N'Hưng Yên'),
 (N'Vật liệu Đại Phát',       '0901000003', N'Hà Nội');

INSERT INTO VatTu(MaVatTu, TenVatTu, DVT, SoLuongTon) VALUES
 ('VT001', N'Xi măng PC40', N'Bao',   0),
 ('VT002', N'Thép phi 10',  N'Kg',    0),
 ('VT003', N'Cát vàng',     N'm3',    0),
 ('VT004', N'Gạch ống',     N'Viên',  0),
 ('VT005', N'Sơn nước',     N'Thùng', 0),
 ('VT006', N'Đinh 5cm',     N'Kg',    0);

-- 1 phiếu nhập mẫu (cập nhật tồn kho khớp với chi tiết)
INSERT INTO PhieuKho(LoaiPhieu, MaNCC) VALUES (N'NHAP', 1);
INSERT INTO ChiTietPhieuKho(MaPhieu, MaVatTu, SoLuong, DonGia) VALUES
 (1,'VT001',100,85000),(1,'VT002',500,18000),(1,'VT003',8,280000),
 (1,'VT004',2000,1200),(1,'VT005',4,1500000);
UPDATE v SET SoLuongTon = v.SoLuongTon + c.SoLuong
FROM VatTu v JOIN ChiTietPhieuKho c ON c.MaVatTu = v.MaVatTu WHERE c.MaPhieu = 1;
UPDATE PhieuKho SET TongTien = (SELECT SUM(SoLuong*DonGia) FROM ChiTietPhieuKho WHERE MaPhieu = 1) WHERE MaPhieu = 1;
GO
