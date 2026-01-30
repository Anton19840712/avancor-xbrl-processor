using System.Collections.ObjectModel;
using XbrlProcessor.Models.Collections;

namespace XbrlProcessor.Models.Entities;

/// <summary>
/// Модель записи контекста
/// </summary>

/* XBRL definition
 * namespace: http://www.xbrl.org/2003/instance
 * file path: www.xbrl.org\2003\xbrl-instance-2003-12-31.xsd

<element name="context">
    <annotation>
        <documentation>
            Used for an island of context to which facts can be related.
        </documentation>
    </annotation>
    <complexType>
        <sequence>
        <element name="entity" type="xbrli:contextEntityType" />
        <element name="period" type="xbrli:contextPeriodType" />
        <element name="scenario" type="xbrli:contextScenarioType" minOccurs="0" />
        </sequence>
        <attribute name="id" type="ID" use="required" />
    </complexType>
</element>

<complexType name="contextEntityType">
    <annotation>
        <documentation>
            The type for the entity element, used to describe the reporting entity.
            Note that the scheme attribute is required and cannot be empty.
        </documentation>
    </annotation>
    <sequence>
        <element name="identifier">
        <complexType>
            <simpleContent>
            <extension base="token">
                <attribute name="scheme" use="required">
                <simpleType>
                    <restriction base="anyURI">
                    <minLength value="1" />
                    </restriction>
                </simpleType>
                </attribute>
            </extension>
            </simpleContent>
        </complexType>
        </element>
        <element ref="xbrli:segment" minOccurs="0" />
    </sequence>
</complexType>

<element name="segment">
    <complexType>
        <sequence>
        <any namespace="##other" processContents="lax"
            minOccurs="1" maxOccurs="unbounded" />
        </sequence>
    </complexType>
</element>

<complexType name="contextPeriodType">
    <annotation>
        <documentation>
            The type for the period element, used to describe the reporting date info.
        </documentation>
    </annotation>
    <choice>
        <sequence>
            <element name="startDate" type="xbrli:dateUnion" />
            <element name="endDate" type="xbrli:dateUnion" />
        </sequence>
        <element name="instant" type="xbrli:dateUnion" />
        <element name="forever">
        <complexType />
        </element>
    </choice>
</complexType>

<simpleType name="dateUnion">
    <annotation>
        <documentation>
            The union of the date and dateTime simple types.
        </documentation>
    </annotation>
    <union memberTypes="date dateTime " />
</simpleType>

*/

public class Context
{
    public required string Id { get; set; }

    /// <summary>Значение контекста</summary>
    /// <value>Строковое значение, максимальная длина 200, обязательное</value>
    /// <see>xbrli:context/xbrli:entity/xbrli:identifier</see>
    public virtual string? EntityValue { get; set; }

    /// <summary>Схема контекста</summary>
    /// <value>Строковое значение, максимальная длина 200, обязательное</value>
    /// <see>xbrli:context/xbrli:entity/xbrli:identifier[@scheme]</see>
    public virtual string? EntityScheme { get; set; }

    /// <summary>Сегмент контекста (значение XBRL)</summary>
    /// <value>Строковое значение, максимальная длина 200, необязательное</value>
    /// <see>xbrli:context/xbrli:entity/xbrli:segment</see>
    public virtual string? EntitySegment { get; set; }

    /// <summary>Дата отчета (значение XBRL)</summary>
    /// <value>Значение даты, необязательное</value>
    /// <see>xbrli:context/xbrli:period/xbrli:instant</see>
    public virtual System.DateTime? PeriodInstant { get; set; }

    /// <summary>Дата начала отчета (для определения периода) (значение XBRL)</summary>
    /// <value>Значение даты, необязательное</value>
    /// <see>xbrli:context/xbrli:period/xbrli:startDate</see>
    public virtual System.DateTime? PeriodStartDate { get; set; }

    /// <summary>Дата окончания отчета (для определения периода) (значение XBRL)</summary>
    /// <value>Значение даты, необязательное</value>
    /// <see>xbrli:context/xbrli:period/xbrli:endDate</see>
    public virtual System.DateTime? PeriodEndDate { get; set; }

    /// <summary>Метка отсутствия периода отчета (бессрочный) (значение XBRL)</summary>
    /// <value>true/false</value>
    /// <see>xbrli:context/xbrli:period/xbrli:forever (complexType)</see>
    public virtual bool PeriodForever { get; set; }

    /// <summary>Коллекция сценариев контекста</summary>
    /// <see>xbrli:context/xbrli:scenario (sequence)</see>
    public virtual Scenarios Scenarios { get; set; } = new Scenarios();
}
