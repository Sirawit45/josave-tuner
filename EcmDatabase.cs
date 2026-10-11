using System;
using System.Collections.Generic;

namespace RemixHondaFlash
{
    public static class EcmDatabase
    {
        public static readonly List<EcmInfo> AllRecords = new List<EcmInfo>();
        public static readonly List<EcmInfo> KeihinRecords = new List<EcmInfo>();
        public static readonly List<EcmInfo> ShindengenRecords = new List<EcmInfo>();

        static EcmDatabase()
        {
            AddKeihin(0, "38770-K2F-N01", "0104080F01", "0", "3FFF8", "Scoopy-i (2021-2024)", 256);
            AddKeihin(1, "38770-MKK-D02", "0102A40101", "0", "FFFF8", "Africa Twin CRF1000L / CRF1100L", 512);
            AddKeihin(2, "38770-KVG-V42", "0100F91103", "4000", "7600", "Honda Generic ECM", 64);
            AddKeihin(3, "38770-K25-901", "0101340F01", "4000", "7600", "Honda Generic ECM", 64);
            AddKeihin(4, "38770-K46-N01", "0101A10F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(5, "38770-K41-N01", "01018B0F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(6, "38770-MFN-D01", "01006A0101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(7, "38770-MFN-F01", "01006A0301", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(8, "38770-MKJ-D02", "0102F60102", "0", "FFFF8", "Honda Generic ECM", 512);
            AddKeihin(9, "38770-MGC-D32", "0101390102", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(10, "38770-K15-903", "01011B0F02", "4000", "7600", "CB150R Streetfire", 64);
            AddKeihin(11, "38770-K15-921", "0101DA0F01", "8000", "0", "CB150R Streetfire", 128);
            AddKeihin(12, "38770-K15-888", "0103130F11", "8000", "0", "CB150R Streetfire", 128);
            AddKeihin(13, "38770-K15-601", "0103130F01", "8000", "0", "CB150R Streetfire", 128);
            AddKeihin(14, "38770-K18-961", "0102E70F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(15, "38770-K0A-R11", "0102A22101", "8000", "0", "CB300R Neo Sports Cafe (2018-2024)", 128);
            AddKeihin(16, "38770-KVK-B31", "0101080801", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(17, "38770-K0A-A01", "0102B70401", "8000", "0", "CB300R Neo Sports Cafe (2018-2024)", 128);
            AddKeihin(18, "38770-MFM-651", "01004C0701", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(19, "38770-MFM-702", "01004C0601", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(20, "38770-MJW-D51", "0101F20101", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(21, "38770-MJW-N41", "0101F20F01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(22, "38770-MJW-B41", "0101F20801", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(23, "38770-MGZ-D02", "0101250101", "0", "3FFF8", "CBR500R / CB500F / CB500X (2013-2015)", 256);
            AddKeihin(24, "38770-MGZ-A03", "0101250501", "0", "3FFF8", "CBR500R / CB500F / CB500X (2013-2015)", 256);
            AddKeihin(25, "38770-MGZ-C03", "0101250B02", "0", "3FFF8", "CBR500R / CB500F / CB500X (2013-2015)", 256);
            AddKeihin(26, "38770-MGZ-C02", "0101250B01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2013-2015)", 256);
            AddKeihin(27, "38770-MJW-D91", "0101DE0101", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(28, "38770-MJW-961", "0101DE0F01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(29, "38770-MJW-NM1", "0102780F01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(30, "38770-MJW-???", "0102780D01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(31, "38770-MFG-D02", "0100390101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(32, "38770-MJE-D41", "01019C0101", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(33, "38770-MJE-TB1", "01027D0D01", "0", "7FFF8", "CBR650F / CB650F (2014-2018)", 512);
            AddKeihin(34, "38770-MJE-G12", "01014E1201", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(35, "38770-MJE-G52", "01019C1201", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(36, "38770-MKN-D51", "0103050101", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(37, "38770-MKN-HF2", "0103BF0E01", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(38, "38770-MFA-D03", "0100300101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(39, "38770-MEL-613", "01000F0202", "0", "3FFFC", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(40, "38770-MEL-622", "01000F0302", "0", "3FFFC", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(41, "38770-MEL-643", "01000F0102", "0", "3FFFC", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(42, "38770-MEL-D01", "01001A0101", "0", "3FFFC", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(43, "38770-MEL-A21", "01002B0401", "0", "3FFF8", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(44, "38770-MEL-A22", "01002B0402", "0", "3FFF8", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(45, "38770-MEL-F21", "01002B0301", "0", "3FFF8", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(46, "38770-MEL-L22", "01002B0502", "0", "3FFF8", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(47, "38770-MEL-D21", "01002B0101", "0", "3FFF8", "CBR1000RR Fireblade (2004-2007)", 256);
            AddKeihin(48, "38770-MFL-644", "01005E0103", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(49, "38770-MFL-702", "01005E0601", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(50, "38770-MFL-622", "01005E0301", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(51, "38770-MFL-773", "01005E0503", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(52, "38770-MFL-671", "01005E0401", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(53, "38770-MFL-741", "0100B00301", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(54, "38770-MFL-691", "0100B00401", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(55, "38770-MFL-861", "0100B00501", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(56, "38770-MFL-871", "0100B00801", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(57, "38770-MFL-761", "0100B00102", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(58, "38770-MFL-J41", "0100B00601", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(59, "38770-MFL-J21", "0100B70601", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(60, "38770-MFL-L21", "0100B70501", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(61, "38770-MFL-F21", "0100B70301", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(62, "38770-MFL-D21", "0100B70101", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(63, "38770-MFL-A21", "0100B70401", "0", "3FFF8", "CBR1000RR Fireblade (2008-2011)", 256);
            AddKeihin(64, "38770-MGP-L01", "0100F30501", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(65, "38770-MGP-F01", "0100F30301", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(66, "38770-MGP-A01", "0100F30401", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(67, "38770-MGP-J01", "0100F30601", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(68, "38770-MGP-D01", "0100F30101", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(69, "38770-MGP-T42", "0101830D01", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(70, "38770-MGP-A92", "0101830401", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(71, "38770-MGP-F63", "0101830302", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(72, "38770-MGP-D62", "0101830101", "0", "3FFF8", "CBR1000RR Fireblade (2012-2016)", 256);
            AddKeihin(73, "38770-MKF-A22", "0102480401", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(74, "38770-MKF-H11", "0102490E01", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(75, "38770-MKF-L13", "0102490501", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(76, "38770-MKF-D43", "0102490101", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(77, "38770-MKFA-A22", "0103380D01", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(78, "38770-MKFA-DK2", "0103380101", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(79, "38770-NLR-B31", "01005EFD74", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(80, "38770-MKFA-D72", "0102850111", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(81, "38770-MKF-D72", "0102850101", "0", "FFFF8", "CBR1000RR SP / Fireblade (2017-2019)", 512);
            AddKeihin(82, "38770-NLS-000", "01028501FF", "0", "FFFF8", "Honda Generic ECM", 512);
            AddKeihin(83, "38770-KTY-H52", "0100C60102", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(84, "38770-K98-E02", "0102A10101", "8000", "0", "CB150R Neo Sports Cafe (2017-2024)", 128);
            AddKeihin(85, "38770-KPP-N02", "0100FA1001", "0", "DFEF", "CBR150R (2011-2015)", 64);
            AddKeihin(86, "38770-KPP-602", "0101110F01", "0", "DFEF", "CBR150R (2011-2015)", 64);
            AddKeihin(87, "38770-K45-N01", "0101B10F01", "8000", "0", "CBR150R LED (2016-2024)", 128);
            AddKeihin(88, "38770-K45-N42", "0102040F01", "8000", "0", "CBR150R LED (2016-2024)", 128);
            AddKeihin(89, "38770-K94-T02", "0102A00D01", "8000", "0", "CBR150R (Indonesia)", 128);
            AddKeihin(90, "38770-K45-NA1", "0103250F01", "8000", "0", "CBR150R LED (2016-2024)", 128);
            AddKeihin(91, "38770-K45-NA1", "0103250F11", "8000", "0", "CBR150R LED (2016-2024)", 128);
            AddKeihin(92, "38770-KPP-T03", "0100C50D02", "0", "DFEF", "CBR150R (2011-2015)", 64);
            AddKeihin(93, "38770-KYJ-922", "0100B80502", "0", "DFEF", "CBR250R (2011-2014)", 64);
            AddKeihin(94, "38770-KYJ-971", "0100B80F01", "0", "DFEF", "CBR250R (2011-2014)", 64);
            AddKeihin(95, "38770-K33-N03", "0101790F02", "E0000", "B000", "CBR300R / CB300F (2014-2020)", 1024);
            AddKeihin(96, "38770-K33-D22", "0101790101", "E0000", "B000", "CBR300R / CB300F (2014-2020)", 1024);
            AddKeihin(97, "38770-KYJ-R11", "0101320671", "0", "18FFE", "CBR250R (2011-2014)", 64);
            AddKeihin(98, "38770-K64-J01", "0101E40601", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(99, "38770-K64-N04", "0101E40F03", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(100, "38770-K64-N01", "0101E40F11", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(101, "38770-K64-NB1", "0103DC0F01", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(102, "38770-K33-D02", "0101740101", "E0000", "B000", "CBR300R / CB300F (2014-2020)", 1024);
            AddKeihin(103, "38770-KOA-T11", "0102B70D02", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(104, "38770-MGZ-U02", "0101250701", "0", "3FFF8", "CBR500R / CB500F / CB500X (2013-2015)", 256);
            AddKeihin(105, "38770-MJW-N11", "0101DD0F01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(106, "38770-MJW-T11", "0101DD0D01", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(107, "38770-MJW-AQ1", "0102F20511", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(108, "38770-MJW-DR1", "0102F20111", "0", "3FFF8", "CBR500R / CB500F / CB500X (2016-2018)", 256);
            AddKeihin(109, "38770-MGM-D11", "0100E00101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(110, "38770-MEE-622", "0100020302", "0", "3FFE0", "Honda Generic ECM", 64);
            AddKeihin(111, "38770-MEE-612", "0100020202", "0", "3FFE0", "Honda Generic ECM", 64);
            AddKeihin(112, "38770-MEE-613", "0100020203", "0", "3FFE0", "Honda Generic ECM", 64);
            AddKeihin(113, "38770-MEE-F02", "01001B0301", "0", "3FFFC", "Honda Generic ECM", 256);
            AddKeihin(114, "38770-MEE-D01", "01001B0101", "0", "3FFFC", "Honda Generic ECM", 256);
            AddKeihin(115, "38770-MEE-A02", "01001B0401", "0", "3FFFC", "Honda Generic ECM", 256);
            AddKeihin(116, "38770-MFJ-D04", "0100330103", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(117, "38770-MFJ-F03", "0100330302", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(118, "38770-MFJ-D42", "01007C0102", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(119, "38770-MJC-D01", "0101420101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(120, "38770-MJC-A01", "0101420401", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(121, "38770-N1A-D00", "0100330171", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(122, "38770-N1A-D11", "0101420171", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(123, "38770-MJE-A02", "01014E0401", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(124, "38770-MJE-T15", "01014E0D03", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(125, "38770-MJE-T52", "01019C0D02", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(126, "38770-MJE-S11", "0102101011", "0", "3FFF8", "CBR650F / CB650F (2014-2018)", 256);
            AddKeihin(127, "38770-MJE-VB1", "01027D0701", "0", "7FFF8", "CBR650F / CB650F (2014-2018)", 512);
            AddKeihin(128, "38770-MJE-BB1", "01027D0101", "0", "7FFF8", "CBR650F / CB650F (2014-2018)", 512);
            AddKeihin(129, "38770-MJE-HB3", "01027D0E02", "0", "7FFF8", "CBR650F / CB650F (2014-2018)", 512);
            AddKeihin(130, "38770-MKN-D11", "01031A0101", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(131, "38770-MKN-U11", "01031A1801", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(132, "38770-MKN-T11", "01031A0D01", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(133, "38770-MKN-HB2", "0103BE0E01", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(134, "38770-MKN-DB1", "01031A1201", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(135, "38770-MKN-L32", "01031A0501", "0", "7FFF8", "CBR650R / CB650R (2019-2024)", 512);
            AddKeihin(136, "38770-MKG-N52", "01025C0F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(137, "38770-MJP-DA1", "0102560101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(138, "38770-KYK-D12", "0102DE0501", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(139, "38770-K28-A62", "0102DD0501", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(140, "38770-K84-901", "0102320F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(141, "38770-K99-B02", "0103160801", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(142, "38770-KZZ-902", "01011A0D01", "E0000", "B000", "CRF250L / CRF250M / CRF250 Rally", 1024);
            AddKeihin(143, "38770-KZZ-914", "01011A0503", "E0000", "B000", "CRF250L / CRF250M / CRF250 Rally", 1024);
            AddKeihin(144, "38770-KZZ-J01", "0102360D01", "8000", "0", "CRF250L / CRF250M / CRF250 Rally", 128);
            AddKeihin(145, "38770-KZZ-J81", "0102360101", "8000", "0", "CRF250L / CRF250M / CRF250 Rally", 128);
            AddKeihin(146, "38770-KZZ-A22", "01015B0D01", "E0000", "B000", "CRF250L / CRF250M / CRF250 Rally", 1024);
            AddKeihin(147, "38770-K95-A41", "0102890101", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(148, "38770-KZZ-L41", "0102360F01", "8000", "0", "CRF250L / CRF250M / CRF250 Rally", 128);
            AddKeihin(149, "38770-K1T-T01", "0104070D01", "8000", "0", "CRF300L / CRF300 Rally (2021-2024)", 128);
            AddKeihin(150, "38770-K1T-A11", "0104070501", "8000", "0", "CRF300L / CRF300 Rally (2021-2024)", 128);
            AddKeihin(151, "38770-MKE-A71", "0102E80501", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(152, "38770-MKE-AF3", "0103B60503", "C000", "0", "Honda Generic ECM", 128);
            AddKeihin(153, "38770-MKE-A81", "0102E90501", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(154, "38770-K47-M01", "0103261801", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(155, "38770-MEW-D41", "01009C0101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(156, "38770-K76-M02", "01029E1801", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(157, "38770-K76-T62", "0102B50D01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(158, "38770-MFT-642", "01008F0101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(159, "38770-MGF-D12", "0100ED0101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(160, "38770-KYZ-T03", "0100FB0E02", "4000", "7600", "Wave 125i ปลาวาฬ (2012-2017)", 64);
            AddKeihin(161, "38770-KYZ-V21", "01010C1101", "4000", "7600", "Wave 125i ปลาวาฬ (2012-2017)", 64);
            AddKeihin(162, "38770-KYZ-V71", "01015F1101", "4000", "7600", "Wave 125i ปลาวาฬ (2012-2017)", 64);
            AddKeihin(163, "38770-K73-V42", "0103141101", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(164, "30400-K0J-N01", "0103110F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(165, "38770-K26-911", "0101350501", "4000", "7600", "MSX 125 / MSX SF / Grom 125 (2013-2020)", 64);
            AddKeihin(166, "38770-K26-931", "0101350101", "4000", "7600", "MSX 125 / MSX SF / Grom 125 (2013-2020)", 64);
            AddKeihin(167, "38770-K26-B13", "0102130501", "8000", "0", "MSX 125 / MSX SF / Grom 125 (2013-2020)", 128);
            AddKeihin(168, "38770-K26-C31", "0102570501", "8000", "0", "MSX 125 / MSX SF / Grom 125 (2013-2020)", 128);
            AddKeihin(169, "38770-K56-N11", "0101CA0F01", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(170, "38770-K56-NJ1", "0103920F01", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(171, "38770-MKA-D71", "0801DF0101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(172, "38770-GGE-941", "01006D1101", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(173, "38770-GGE-901", "0100B41101", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(174, "38770-KYE-942", "0101700F01", "4000", "7600", "Honda Generic ECM", 64);
            AddKeihin(175, "38770-K0F-A01", "0102CA0501", "8000", "0", "Monkey 125 (2018-2020)", 128);
            AddKeihin(176, "38770-MJL-Y11", "0101820101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(177, "38770-MCG-602", "0101220801", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(178, "38770-K87-T52", "01025A0D01", "E0000", "B000", "Rebel 300 (2017-2024)", 1024);
            AddKeihin(179, "38770-K03-N32", "0101870F02", "8000", "0", "Wave 110i (2019-2020)", 128);
            AddKeihin(180, "38770-K58-N41", "0102E40F01", "8000", "0", "Wave 110i / Wave 125i (2020-2022)", 128);
            AddKeihin(181, "38770-KPG-H01", "0102D71F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(182, "38770-K56-M01", "0101CD1801", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(183, "38770-K56-M91", "0103931801", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(184, "38770-HR6-MO2", "0D02FC0401", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(185, "30400-K2F-N11", "0104130F01", "0", "3FFF8", "Scoopy-i (2021-2024)", 256);
            AddKeihin(186, "30400-K93-N01", "01027F0F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(187, "30400-K93-T01", "0102810D01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(188, "38770-GFM-601", "01006B0801", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(189, "38770-KTW-903", "0100450103", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(190, "38770-KTW-B02", "0100530101", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(191, "38770-K53-DB2", "0102B10101", "0", "DFEF", "Honda Generic ECM", 64);
            AddKeihin(192, "38770-K53-D03", "0101B80103", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(193, "38770-K56-N01", "0101C90F01", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(194, "38770-KZL-E12", "0101021101", "4000", "7600", "Spacy-i / Zoomer-X (2012-2014)", 64);
            AddKeihin(195, "38770-KZL-C31", "01011E0F01", "4000", "7600", "Spacy-i / Zoomer-X (2012-2014)", 64);
            AddKeihin(196, "38770-K0G-962", "01030E0501", "8000", "0", "Super Cub 110 Pro", 128);
            AddKeihin(197, "38770-KZV-A61", "0101BA0D01", "8000", "0", "Dream Super Cub 110 (2013-2017)", 128);
            AddKeihin(198, "38770-KYZ-713", "0100E80F02", "4000", "7600", "Wave 125i ปลาวาฬ (2012-2017)", 64);
            AddKeihin(199, "38770-HL6-A05", "0801EB0502", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(200, "30400-K46-N21", "0101FA0F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(201, "30400-K46-N31", "0101F80F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(202, "30400-K46-NA1", "0102F80F01", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(203, "38770-K18-902", "0101200F01", "4000", "7600", "Honda Generic ECM", 64);
            AddKeihin(204, "38770-K18-941", "0101F30F01", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(205, "38770-MGE-623", "0100AA0301", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(206, "38770-MGE-D01", "0101030101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(207, "38770-MGE-F11", "0101030301", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(208, "38770-MGE-D41", "0101190101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(209, "38770-MGE-F02", "0800AB0301", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(210, "38770-MGE-D02", "0800AB0101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(211, "38770-MGH-641", "0100FC0101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(212, "38770-MGH-D21", "0801040101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(213, "38770-MGH-F21", "0801040301", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(214, "38770-MGH-N12", "08017D0101", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(215, "38770-MGH-M12", "08017D0301", "0", "7FFF8", "Honda Generic ECM", 512);
            AddKeihin(216, "38770-MFR-642", "01007B0101", "0", "3FFF8", "Honda Generic ECM", 256);
            AddKeihin(217, "38770-KWW-643", "0100CE0D02", "4000", "7600", "Wave 110i (2009-2018)", 64);
            AddKeihin(218, "38770-K03-H01", "0101880D01", "8000", "0", "Wave 110i (2019-2020)", 128);
            AddKeihin(219, "38770-G90-V02", "0102681101", "8000", "0", "Honda Generic ECM", 128);
            AddKeihin(220, "38770-K58-TC2", "01034C0D01", "8000", "0", "Wave 110i / Wave 125i (2020-2022)", 128);
            AddKeihin(221, "38770-K58-T81", "0102B90D01", "8000", "0", "Wave 110i / Wave 125i (2020-2022)", 128);
            AddKeihin(222, "38770-K73-M41", "0102AD1801", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(223, "38770-K56-V01", "0101CB1101", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(224, "38770-K56-V51", "0103591101", "8000", "0", "Sonic 150R / Winner 150 / Supra GTR 150", 128);
            AddKeihin(225, "38770-MKH-D01", "08024C0101", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(226, "38770-MKH-D22", "08024CFFFF", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(227, "38770-MKH-C02", "0802E60E01", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(228, "38770-MKH-C03", "0802E60E02", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(229, "38770-MKH-D23", "0802E60102", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(230, "38770-MKH-D42", "08024CFFFE", "0", "7FFF8", "X-ADV 750 / Forza 750 / NC750X (2017-2024)", 512);
            AddKeihin(231, "38770-KWT-703", "0101310802", "E0000", "B000", "Honda Generic ECM", 1024);
            AddKeihin(232, "38770-K73-M62", "01033F1801", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(233, "38770-K3F-T01", "01047B0D01", "8000", "0", "Wave 125i (2023-2024)", 128);
            AddKeihin(234, "38770-KYZ-T41", "0101B30D01", "8000", "0", "Wave 125i ปลาวาฬ (2012-2017)", 128);
            AddKeihin(235, "38770-K73-T32", "0102580D01", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(236, "38770-K73-T61", "01033E0D01", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(237, "38770-K73-TC2", "01034F1801", "8000", "0", "Wave 125i LED (2018-2022)", 128);
            AddKeihin(238, "38770-K2J-TH1", "0105550D01", "8000", "0", "Wave 110i / Super Cub (2021-2024)", 128);
            AddKeihin(239, "38770-K2J-T01", "01040F0D01", "8000", "0", "Wave 110i / Super Cub (2021-2024)", 128);
            AddKeihin(240, "38770-K1M-T01", "0103FA0D01", "8000", "0", "Wave 110i AT", 128);
            AddKeihin(241, "38770-K26-B02", "0102130D01", "8000", "0", "MSX 125 / MSX SF / Grom 125 (2013-2020)", 128);
            AddKeihin(243, "38770-KWW-641", "0100CE0D01", "4000", "7600", "Wave 110i (2009-2018)", 64);
            AddKeihin(244, "38770-KWW-642", "0100CE0D01", "4000", "7600", "Wave 110i (2009-2018)", 64);
            AddKeihin(245, "38770-KWW-643", "0100CE0D01", "4000", "7600", "Wave 110i (2009-2018)", 64);
            AddKeihin(246, "38770-K03-T61", "0101610D01", "4000", "7600", "Wave 110i (2019-2020)", 64);
            AddKeihin(247, "38770-K04-901", "01016A0101", "0", "7FFF8", "Forza 300 (2013-2017)", 512);
            AddKeihin(248, "38770-K0B-T01", "0102DA0D01", "0", "7FFF8", "Forza 300 (2018-2020)", 512);
            AddKeihin(249, "38770-K1B-T01", "0103E80D01", "0", "7FFF8", "Forza 350 / ADV 350 (2020-2022)", 512);
            AddKeihin(250, "38770-K1B-T02", "01041A0D01", "0", "7FFF8", "Forza 350 / ADV 350 (2022-2023)", 512);
            AddKeihin(251, "38770-K1B-T21", "01049B0D01", "0", "7FFF8", "Forza 350 (2023-2024)", 512);
            AddKeihin(252, "38770-K1B-T61", "01053A0D01", "0", "7FFF8", "Forza 350 (2024-2025 TFT)", 512);
            AddShindengen(0, "30400-K1ZF-T01 PCX160 NOABS", "0103D00D01", "PCX 160 e:HEV / ABS (2021-2024)", 512);
            AddShindengen(1, "30400-K1ZF-T11 PCX160ABS", "0103CF0D01", "PCX 160 e:HEV / ABS (2021-2024)", 512);
            AddShindengen(2, "30400-K1ZP-T51 PCX160 NOABS", "0105050D01", "PCX 160 e:HEV / ABS (2021-2024)", 512);
            AddShindengen(3, "30400-K1ZP-T71 PCX160 ABS", "0105040D01", "PCX 160 e:HEV / ABS (2021-2024)", 512);
            AddShindengen(4, "30400-K2TA-T02 LEAD125 4V", "0104780D01", "Lead 125 4V (2022-2024)", 512);
            AddShindengen(5, "30400-K0WF-T01 ADV150", "01037C0D01", "ADV 150 / ADV 160 (2019-2024)", 384);
            AddShindengen(6, "30400-K0WM-TA1 ADV160", "01048E0D01", "ADV 150 / ADV 160 (2019-2024)", 512);
            AddShindengen(7, "30400-K2SF-T01 CLICK160", "01046D0D01", "Scoopy-i (2013-2016)", 512);
            AddShindengen(8, "30400-K2VH-T11 CLICK125 2023", "0104DC0D01", "Click 125i (2023-2024)", 384);
            AddShindengen(9, "30400-K3MF-T01 GIONRO 125", "01052A0D01", "Giorno+ 125 (2023-2024)", 512);
            AddShindengen(10, "30400-K20J-T21 Zoomer-X 2015-2017", "0101FF0D01", "Zoomer-X (2015-2020)", 256);
            AddShindengen(11, "30400-K36F-T01 PCX150 2014-2015", "0101990D01", "PCX 150 (2014-2017)", 256);
            AddShindengen(12, "30400-K36S-T31  PCX150 2016-2017", "0102180D01", "PCX 150 (2014-2017)", 384);
            AddShindengen(13, "30400-K59K-T11 Click150 2018-2020", "0103070D01", "Click 150i (2018-2022)", 384);
            AddShindengen(14, "30400-K60F-T01 Click125 2014-2015", "0101D00D01", "Click 125i LED (2018-2022)", 256);
            AddShindengen(15, "30400-K60S-T71 Click125 2016-2017", "01030A0D01", "Click 125i LED (2018-2022)", 384);
            AddShindengen(16, "30400-K97F-T PCX150 2017-2019", "010102990D01", "PCX 150 Smart Key (2018-2020)", 384);
            AddShindengen(17, "30400-K0WF-T01 ADV150 2017-2019", "01037C0D01", "ADV 150 / ADV 160 (2019-2024)", 384);
            AddShindengen(18, "30400-K0WM-TA1 ADV160", "01048E0D01", "ADV 150 / ADV 160 (2019-2024)", 512);
        }

        private static void AddKeihin(int idx, string part, string id, string sStart, string sCk, string model, int romKb)
        {
            var info = new EcmInfo
            {
                Index = idx,
                PartCode = part,
                EcmId = id,
                StartOffset = ParseHex(sStart),
                ChecksumOffset = ParseHex(sCk),
                Vendor = EcuVendor.Keihin,
                ModelName = model,
                RomSizeKb = romKb
            };
            KeihinRecords.Add(info);
            AllRecords.Add(info);
        }

        private static void AddShindengen(int idx, string part, string id, string model, int romKb)
        {
            var info = new EcmInfo
            {
                Index = idx,
                PartCode = part,
                EcmId = id,
                StartOffset = 0,
                ChecksumOffset = 0,
                Vendor = EcuVendor.Shindengen,
                ModelName = model,
                RomSizeKb = romKb
            };
            ShindengenRecords.Add(info);
            AllRecords.Add(info);
        }

        public static int ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return 0;
            hex = hex.Replace("0x", "").Replace("0X", "").Trim();
            if (hex.Length == 0) return 0;
            try {
                return Convert.ToInt32(hex, 16);
            } catch {
                return 0;
            }
        }

        public static EcmInfo LookupByEcmId(string ecmId)
        {
            if (string.IsNullOrEmpty(ecmId)) return null;
            ecmId = ecmId.Replace("-", "").ToUpperInvariant();

            foreach (var r in ShindengenRecords)
            {
                if (r.EcmId.Replace("-", "").ToUpperInvariant().Contains(ecmId) || ecmId.Contains(r.EcmId.Replace("-", "").ToUpperInvariant()))
                    return r;
            }

            foreach (var r in KeihinRecords)
            {
                if (r.EcmId.Replace("-", "").ToUpperInvariant().Contains(ecmId) || ecmId.Contains(r.EcmId.Replace("-", "").ToUpperInvariant()))
                    return r;
            }

            return null;
        }

        public static EcmInfo LookupByPartCode(string partCode)
        {
            if (string.IsNullOrEmpty(partCode)) return null;
            string clean = partCode.Replace("-", "").ToUpperInvariant();

            foreach (var r in AllRecords)
            {
                if (r.PartCode.Replace("-", "").ToUpperInvariant().Contains(clean) || clean.Contains(r.PartCode.Replace("-", "").ToUpperInvariant()))
                    return r;
            }

            return null;
        }

        public static List<EcmInfo> Search(string query)
        {
            List<EcmInfo> results = new List<EcmInfo>();
            if (string.IsNullOrEmpty(query)) return new List<EcmInfo>(AllRecords);

            query = query.ToLowerInvariant();
            foreach (var r in AllRecords)
            {
                if (r.ModelName.ToLowerInvariant().Contains(query) ||
                    r.PartCode.ToLowerInvariant().Contains(query) ||
                    r.EcmId.ToLowerInvariant().Contains(query))
                {
                    results.Add(r);
                }
            }
            return results;
        }

        public static EcuType DetectEcuTypeFromBin(byte[] binBytes)
        {
            if (binBytes == null || binBytes.Length == 0) return EcuType.Unknown;

            // Check Shindengen V850
            byte[] shV850Sig = new byte[] { 0x53, 0x56, 0x38, 0x35, 0x30 }; // 'SV850'
            if (ChecksumEngine.PatternSearch(binBytes, shV850Sig) >= 0) return EcuType.ShindengenV850;

            // Check Shindengen RH850
            byte[] shRh850Sig = new byte[] { 0x53, 0x48, 0x38, 0x35, 0x30 }; // 'SH850'
            if (ChecksumEngine.PatternSearch(binBytes, shRh850Sig) >= 0) return EcuType.ShindengenRH850;

            // Check Keihin H8C
            byte[] khH8cSig = new byte[] { 0xFF, 0xFF, 0xAA, 0xAA };
            if (ChecksumEngine.PatternSearch(binBytes, khH8cSig) >= 0) return EcuType.KeihinH8C;

            // Check Keihin M32R
            byte[] khM32rSig = new byte[] { 0xFF, 0xFF, 0xFF, 0xAA, 0xAA, 0xAA, 0xAA };
            if (ChecksumEngine.PatternSearch(binBytes, khM32rSig) >= 0) return EcuType.KeihinM32R;

            // Shindengen General check (shWKeysig)
            if (ChecksumEngine.PatternSearch(binBytes, SeedKeyEngine.ShindengenWriteKeySig) >= 0)
                return EcuType.ShindengenRH850;

            return EcuType.KeihinOther;
        }

        public static bool DetectOffsets(byte[] binBytes, EcuType ecuType, out int startOffset, out int checksumOffset)
        {
            startOffset = 0;
            checksumOffset = 0;

            if (binBytes == null || binBytes.Length == 0) return false;

            if (ecuType == EcuType.ShindengenV850 || ecuType == EcuType.ShindengenRH850 || ecuType == EcuType.ShindengenRH850B)
            {
                startOffset = 0;
                return ChecksumEngine.FixShindengenChecksum(binBytes, out checksumOffset);
            }

            // Keihin offsets by binary size / structure
            if (binBytes.Length == 0x10000) // 64KB
            {
                startOffset = 0x8000;
                checksumOffset = 0x7600;
            }
            else if (binBytes.Length == 0x20000) // 128KB
            {
                startOffset = 0x8000;
                checksumOffset = 0xDFEF;
            }
            else if (binBytes.Length == 0x40000) // 256KB
            {
                startOffset = 0x0;
                checksumOffset = 0x3FFF8;
            }
            else if (binBytes.Length == 0x80000) // 512KB
            {
                startOffset = 0x0;
                checksumOffset = 0x7FFF8;
            }
            else if (binBytes.Length == 0x100000) // 1MB
            {
                startOffset = 0x0;
                checksumOffset = 0xFFFF8;
            }

            return true;
        }
    }
}
