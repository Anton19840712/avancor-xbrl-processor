using System.Collections.ObjectModel;

namespace Models.XBRL
{

    /// <summary>
    /// Модель класса Unit
    /// </summary>
    /// <remarks>
    /// Определение XSD-схемы в XBRL
    /// namespace: http://www.xbrl.org/2003/instance
    ///  file path: www.xbrl.org\2003\xbrl-instance-2003-12-31.xsd
    ///  
    ///<element name="unit">
    ///    <annotation>
    ///        <documentation>
    ///            Element used to represent units information about numeric items
    ///        </documentation>
    ///    </annotation>
    ///    <complexType>
    ///        <choice>
    ///        <element ref="xbrli:measure" minOccurs="1" maxOccurs="unbounded" />
    ///        <element ref="xbrli:divide" />
    ///        </choice>
    ///        <attribute name = "id" type="ID" use="required" />
    ///    </complexType>
    ///</element>
    ///
    ///<element name = "measure" type="QName" />
    ///
    ///<complexType name = "measuresType" >
    ///    < annotation >
    ///        < documentation >
    ///            A collection of sibling measure elements
    ///        </documentation>
    ///    </annotation>
    ///    <sequence>
    ///        <element ref="xbrli:measure" minOccurs="1" maxOccurs="unbounded" />
    ///    </sequence>
    ///</complexType>
    ///
    ///<element name = "divide" >
    ///    < annotation >
    ///        < documentation >
    ///            Element used to represent division in units
    ///        </documentation>
    ///    </annotation>
    ///    <complexType>
    ///        <sequence>
    ///            <element name = "unitNumerator" type="xbrli:measuresType" />
    ///            <element name = "unitDenominator" type="xbrli:measuresType" />
    ///        </sequence>
    ///    </complexType>
    ///</element>
    ///
    /// </remarks>

    public class Unit
    {
        public string Id { get; set; }
        /// <summary>Парметр Measure (значение XBRL)</summary>
        /// <value>Строковое значение, максимальная длина 200, обязательое</value>
        /// <see>xbrli:unit/xbrli:measure</see>
        public virtual string Measure { get; set; }

        /// <summary>Парметр Numerator (значение XBRL)</summary>
        /// <value>Строковое значение, максимальная длина 200, необязательое</value>
        /// <see>xbrli:unit/xbrli:unitNumerator</see>
        public virtual string Numerator { get; set; }

        /// <summary>Парметр unitDenominator (значение XBRL)</summary>
        /// <value>Строковое значение, максимальная длина 200, необязательое</value>
        /// <see>xbrli:unit/xbrli:unitDenominator</see>
        public virtual string Denominator { get; set; }

    }

    public class Units : Collection<Unit>
    {
    }

}
