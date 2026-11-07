# Mapeamento da API Ploomes (endpoints)

Fonte: https://developers.ploomes.com/json/openapi.json (publico). Servidor: https://public-api2.ploomes.com. OpenAPI 3.0.1. Autenticacao: header User-Key.

## Account

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Account` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| PATCH | `/Account({Id})` |  | *Id, $select, $expand, User-Key |

## Cities

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Cities` |  | $top, $skip, $select, $expand, $filter, $orderby |
| GET | `/Cities@Countries` |  | $top, $skip, $select, $expand, $filter, $orderby |
| GET | `/Cities@Countries@States` |  | $top, $skip, $select, $expand, $filter, $orderby |

## Contacts

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Contacts` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Contacts` |  | $select, $expand, User-Key |
| DELETE | `/Contacts({Id})` |  | Id, User-Key |
| PATCH | `/Contacts({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Contacts@NumbersOfEmployees` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Contacts@NumbersOfEmployees` |  | $select, User-Key |
| DELETE | `/Contacts@NumbersOfEmployees({Id})` |  | Id, User-Key |
| PATCH | `/Contacts@NumbersOfEmployees({Id})` |  | Id, $select, User-Key |
| GET | `/Contacts@Origins` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Contacts@Origins` |  | $select |
| DELETE | `/Contacts@Origins({Id})` |  | Id, User-Key |
| PATCH | `/Contacts@Origins({Id})` |  | Id, $select, User-Key |
| GET | `/Contacts@LinesOfBusiness` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Contacts@LinesOfBusiness` |  | $select, User-Key |
| DELETE | `/Contacts@LinesOfBusiness({Id})` |  | Id, User-Key |
| PATCH | `/Contacts@LinesOfBusiness({Id})` |  | Id, $select, User-Key |
| GET | `/Contacts@Relationships` |  |  |
| GET | `/Contacts@Status` |  |  |
| GET | `/Contacts@Types` |  |  |
| GET | `/Contacts@Products` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Contacts@Products` |  | $select, $expand, User-Key |
| DELETE | `/Contacts@Products({Id})` |  | Id, User-Key |
| PATCH | `/Contacts@Products({Id})` |  | Id, $select, $expand, User-Key |

## Currencies

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Currencies` |  |  |

## Deals

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Deals` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Deals` |  | $select, $expand, User-Key |
| DELETE | `/Deals({Id})` |  | Id, User-Key |
| PATCH | `/Deals({Id})` |  | Id, $select, $expand, User-Key |
| POST | `/Deals({Id})/Win` |  | Id, $select, $expand, User-Key |
| POST | `/Deals({Id})/Lose` |  | Id, $select, $expand, User-Key |
| POST | `/Deals({Id})/Reopen` |  | Id, $select, $expand, User-Key |
| GET | `/Deals@Stages` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| GET | `/Deals@Status` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| GET | `/Deals@LossReasons` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| GET | `/Deals@Pipelines` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Deals@Pipelines` |  | $select, $expand, User-Key |
| DELETE | `/Deals@Pipelines({Id})` |  | Id, User-Key |
| PATCH | `/Deals@Pipelines({Id})` |  | Id, $select, $expand, User-Key |

## Documents

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Documents` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Documents` |  | $select, $expand, User-Key |
| DELETE | `/Documents({Id})` |  | Id, User-Key |
| PATCH | `/Documents({Id})` |  | Id, $select, $expand, User-Key |

## DocumentTemplates

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/DocumentTemplates` |  | $top, $skip, $select, $expand, $filter, $orderby |

## Departments

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Departments` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Departments` |  | $select, User-Key |
| DELETE | `/Departments({Id})` |  | Id, User-Key |
| PATCH | `/Departments({Id})` |  | Id, $select, User-Key |

## Fields

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Fields` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Fields` |  | $select, $expand |
| PATCH | `/Fields({Key})` |  | Key, $select, $expand, User-Key |
| DELETE | `/Fields({Key})` |  | Key, User-Key |
| GET | `/Fields@Entities` |  |  |
| GET | `/Fields@OptionsTables` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Fields@OptionsTables` |  | $select, $expand, User-Key |
| GET | `/Fields@OptionsTables@Options` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Fields@OptionsTables@Options` |  | $select, $expand |
| PATCH | `/Fields@OptionsTables@Options({Id})` |  | Id, $select, $expand, User-Key |
| DELETE | `/Fields@OptionsTables@Options({Id})` |  | Id, User-Key |
| GET | `/Fields@Types` |  |  |

## InteractionRecords

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/InteractionRecords` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/InteractionRecords` |  | $select, $expand, User-Key |
| DELETE | `/InteractionRecords({Id})` |  | Id, User-Key |
| PATCH | `/InteractionRecords({Id})` |  | Id, $select, $expand, User-Key |

## Operations

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Operations` |  |  |

## Orders

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Orders` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Orders` |  | $select, $expand, User-Key |
| DELETE | `/Orders({Id})` |  | Id, User-Key |
| PATCH | `/Orders({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Orders@Stages` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Orders@Stages` |  | $select, User-Key |
| DELETE | `/Orders@Stages({Id})` |  | Id, User-Key |
| PATCH | `/Orders@Stages({Id})` |  | Id, $select, User-Key |

## PhoneTypes

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/PhoneTypes` |  |  |

## Products

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Products` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Products` |  | $select, $expand |
| DELETE | `/Products({Id})` |  | Id, User-Key |
| PATCH | `/Products({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Products@Families` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Products@Families` |  | $select, $expand |
| DELETE | `/Products@Families({Id})` |  | Id, User-Key |
| PATCH | `/Products@Families({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Products@Groups` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Products@Groups` |  | $select, $expand |
| DELETE | `/Products@Groups({Id})` |  | Id, User-Key |
| PATCH | `/Products@Groups({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Products@Parts` |  | $top, $skip, $select, $expand, $filter, $orderby |
| POST | `/Products@Parts` |  | $select, $expand |
| DELETE | `/Products@Parts({Id})` |  | Id, User-Key |
| PATCH | `/Products@Parts({Id})` |  | Id, $select, $expand, User-Key |

## Quotes

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Quotes` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Quotes` |  | $select, $expand, User-Key |
| DELETE | `/Quotes({Id})` |  | Id, User-Key |
| PATCH | `/Quotes({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Quotes@ApprovalStatus` |  |  |
| POST | `/Quotes({Id})/Review` |  | Id, $select, $expand, User-Key |

## RelativeDates

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/RelativeDates` |  |  |

## Roles

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Roles` |  | $top, $skip, $select, $filter, $orderby, User-Key |
| POST | `/Roles` |  | $select, User-Key |

## Tags

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Tags` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Tags` |  | $select, $expand, User-Key |
| DELETE | `/Tags({Id})` |  | Id, User-Key |
| PATCH | `/Tags({Id})` |  | Id, $select, $expand, User-Key |

## Tasks

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Tasks` |  | $top, $skip, $select, $expand, $filter, $orderby, User-Key |
| POST | `/Tasks` |  | $select, $expand, User-Key |
| DELETE | `/Tasks({Id})` |  | Id, User-Key |
| PATCH | `/Tasks({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Tasks@Types` |  |  |
| GET | `/Tasks@RepeatIntervalUnits` |  |  |
| GET | `/Tasks@EmailReminders` |  |  |
| POST | `/Tasks({Id})/Finish` |  | Id, User-Key |

## Teams

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Teams` |  | $top, $skip, $select, $expand, $filter, User-Key |
| POST | `/Teams` |  | $select, $expand, User-Key |
| DELETE | `/Teams({Id})` |  | Id, User-Key |
| PATCH | `/Teams({Id})` |  | Id, $select, $expand, User-Key |

## Users

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Users` |  | $top, $skip, $select, $expand, $filter, User-Key |
| POST | `/Users` |  | $select, $expand, User-Key |
| DELETE | `/Users({Id})` |  | Id, User-Key |
| PATCH | `/Users({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Users@Profiles` |  | $top, $skip, $select, $expand, $filter, User-Key |

## Webhooks

| Metodo | Path | Resumo | Parametros |
|---|---|---|---|
| GET | `/Webhooks` |  | $top, $skip, $select, $expand, $filter, User-Key |
| POST | `/Webhooks` |  | $select, $expand, User-Key |
| DELETE | `/Webhooks({Id})` |  | Id, User-Key |
| PATCH | `/Webhooks({Id})` |  | Id, $select, $expand, User-Key |
| GET | `/Webhooks@Actions` |  |  |

