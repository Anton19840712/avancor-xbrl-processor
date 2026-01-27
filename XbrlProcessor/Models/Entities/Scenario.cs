namespace XbrlProcessor.Models.Entities
{

    /// <summary>
    /// Модель записи сценария контекста
    /// </summary>
    
    /* XBRL definition
     
     * Instance
     * Namespace: http://www.xbrl.org/2003/instance
     * File path: www.xbrl.org\2003\xbrl-instance-2003-12-31.xsd
     
        <complexType name="contextScenarioType">
            <annotation>
                <documentation>
                Used for the scenario under which fact have been reported.
                </documentation>
            </annotation>
            <sequence>
                <any namespace="##other" processContents="lax" 
                minOccurs="1" maxOccurs="unbounded" />
            </sequence>
        </complexType>

     * XBRL Dimensions
     * Namespace: http://xbrl.org/2006/xbrldi
     * File path: www.xbrl.org\2006\xbrldi-2006.xsd
     
        <element name="explicitMember">
	        <annotation>
		        <documentation xml:lang="en">This element contains the QName of an item that is a member of an explicit dimension.
            </documentation>
	        </annotation>
	        <complexType>
		        <simpleContent>
			        <extension base="QName">
				        <attribute name="dimension" type="QName" use="required"/>
			        </extension>
		        </simpleContent>
	        </complexType>
        </element>
        <element name="typedMember">
	        <annotation>
		        <documentation xml:lang="en">This element constains one child of anyType.
            </documentation>
	        </annotation>
	        <complexType>
		        <sequence>
			        <any namespace="##other"/>
		        </sequence>
		        <attribute name="dimension" type="QName" use="required"/>
	        </complexType>
        </element>

     * Dimensions library
     * Namespace: http://www.cbr.ru/xbrl/udr/dim/dim-int
     * File path: www.cbr.ru/xbrl/udr/dim/dim-int.xsd

    */

    public class Scenario
    {

        /// <summary>Тип измерерия XBRL</summary>
        /// <value>Строкое значение, максимальная длина 200, обязательное</value>
        /// <see>xbrli:context/xbrli:scenario/(*)[name]: explicitMember, typedMember</see>
        public virtual string DimensionType { get; set; } = default;

        /// <summary>Наименование измерерия XBRL</summary>
        /// <value>Строкое значение, максимальная длина 200, необязательное</value>
        /// <see>xbrli:context/xbrli:scenario/(*)/[@dimension]</see>
        public virtual string DimensionName { get; set; }

        /// <summary>Код измерерия XBRL</summary>
        /// <value>Строкое значение, максимальная длина 500, необязательное</value>
        /// <see>xbrli:context/xbrli:scenario/xbrldi:typedMember/(*)[name]</see>
        public virtual string DimensionCode { get; set; }

        /// <summary>Значение измерерия XBRL (dimension)</summary>
        /// <value>Строкое значение, максимальная длина 500, обязательное, индексируемое</value>
        /// <see>xbrli:context/xbrli:scenario/xbrldi:explicitMember</see>
        /// <see>xbrli:context/xbrli:scenario/xbrldi:typedMember/(*)</see>
        public virtual string DimensionValue { get; set; }

    }

}
