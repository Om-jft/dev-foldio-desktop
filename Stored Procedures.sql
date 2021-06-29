Create Table LicenseInfo (
	ID int IDENTITY(101,1) PRIMARY KEY,
	SoftwateSerialNumber varchar(16) NOT NULL,
	StartDate Date,
	LisenceStatus varchar(7)
)

