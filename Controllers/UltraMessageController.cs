using System;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace WebHookExample.Properties
{
    [Route("api/")]
    [ApiController]
    public class UltraMessageController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromBody] HookData data) 
        {
            Console.WriteLine("Api ok");
            return Ok(data);
        }
        [HttpPost]
        public IActionResult Post([FromBody] HookData data)
        {
            Console.WriteLine(JsonSerializer.Serialize(data));

            try
            {
                string numeroClienteSeparado;
                string pattern = @"^\d{2}|\@.*$";

                if (data.Data.FromMe == true)
                {
                    numeroClienteSeparado = data.Data.To;
                }
                else
                {
                    numeroClienteSeparado = data.Data.From;
                }

                numeroClienteSeparado = Regex.Replace(numeroClienteSeparado, pattern, "");


                string insertOnTelefonesRep = "INSERT INTO zap.zap_contato (numerocli) VALUES (@numero) ON CONFLICT(numerocli) DO UPDATE SET dataultimamsg = NOW()";

                var cmdTel = new NpgsqlCommand(insertOnTelefonesRep);
                cmdTel.Parameters.AddWithValue("@numero", numeroClienteSeparado);
                Modulo.SQL_executeNonQuery(cmdTel);


                if (data.EventType.Equals("message_reaction"))
                {
                    string updateQuery = "UPDATE zap.msgzap SET reaction = @reaction WHERE dataid = @msgId";
                    var cmd = new NpgsqlCommand(updateQuery);
                    string msgEncodada = Modulo.EncodeNonAsciiCharacters(data.Data.Body);
                    cmd.Parameters.AddWithValue("reaction", msgEncodada);
                    cmd.Parameters.AddWithValue("msgId", data.Data.msgId);
                    int resultado = Modulo.SQL_executeNonQuery(cmd);
                    if (resultado == 1)
                    {
                        Console.WriteLine("Reaction adicionado com sucesso as " + DateTime.Now.ToString());
                    }
                }
                else if (data.EventType.Equals("message_ack"))
                {

                    int sqlId = Convert.ToInt32(data.referenceId);
                    Console.WriteLine("Id SQL: " + sqlId);


                    string updateQuery = "UPDATE zap.msgzap SET instanceid = @instanceid, dataid = @dataid, datafrom = @datafrom, datato = @datato, dataack = @ack, datatype = @datatype, databody = @databody, datafromme = @datafromme, dataisforwarded = @dataisforwarded, datatime = @datatime, numerocli = @numerocli, reaction = @reaction WHERE id = @id";
                    string msgEncodada = Modulo.EncodeNonAsciiCharacters(data.Data.Body);



                    var cmd = new NpgsqlCommand(updateQuery);
                    cmd.Parameters.AddWithValue("instanceid", data.InstanceId);
                    cmd.Parameters.AddWithValue("dataid", data.Data.Id);
                    cmd.Parameters.AddWithValue("datafrom", data.Data.From);
                    cmd.Parameters.AddWithValue("datato", data.Data.To);
                    cmd.Parameters.AddWithValue("dataack", data.Data.Ack);
                    cmd.Parameters.AddWithValue("datatype", data.Data.Type);
                    cmd.Parameters.AddWithValue("databody", msgEncodada);
                    cmd.Parameters.AddWithValue("datafromme", data.Data.FromMe);
                    cmd.Parameters.AddWithValue("dataisforwarded", data.Data.IsForwarded);
                    cmd.Parameters.AddWithValue("datatime", Modulo.GetUnixToDate(data.Data.Time));
                    cmd.Parameters.AddWithValue("numerocli", numeroClienteSeparado);
                    cmd.Parameters.AddWithValue("reaction", "");
                    cmd.Parameters.AddWithValue("ack", data.Data.Ack);
                    cmd.Parameters.AddWithValue("msgId", data.Data.Id);
                    cmd.Parameters.AddWithValue("id", sqlId);


                    int resultado = Modulo.SQL_executeNonQuery(cmd);
                    if (resultado == 1)
                    {

                        Console.WriteLine("Mensagem adicionada ou atualizada com sucesso as " + DateTime.Now.ToString());
                    }

                }
                else if (data.EventType.Equals("message_create"))
                {
                    Console.WriteLine("-------------- SQL Pulou o Create. -------------------");
                }
                else
                {
                    string InsertQuery = "INSERT INTO zap.msgzap(event_type, instanceid,dataid, datafrom, datato, dataack, datatype, databody, datamedia, datafromme, dataisforwarded, datatime, numerocli, reaction, atendente) VALUES (@event, @instanceid, @dataid, @datafrom, @datato, @dataack, @datatype, @databody, @datamedia, @datafromme, @dataisforwarded, @datatime, @numerocli, @reaction, @atendente)";
                    string msgEncodada = Modulo.EncodeNonAsciiCharacters(data.Data.Body);

                    string localMedia = "";
                    string pastaDestino = "";

                    if (data.Data.Type.Equals("document") || data.Data.Type.Equals("image") || data.Data.Type.Equals("video") || data.Data.Type.Equals("ptt") || data.Data.Type.Equals("sticker"))
                    {
                        Thread downloadThread = new Thread(() =>
                        {
                            System.Net.WebClient wc = new System.Net.WebClient();
                            var url = data.Data.Media;
                            // Hash com o nome
                            string fileName = url.Substring(url.LastIndexOf("/") + 1), myStringWebResource = null;

                            // Extensao do arquivo
                            string datas = wc.DownloadString(url);
                            string contentType = wc.ResponseHeaders["Content-Type"];
                            if (contentType.Substring(contentType.LastIndexOf("/") + 1).Equals("plain"))
                            {
                                fileName += ".txt";
                            }
                            else if (data.Data.Type.Equals("ptt"))
                            {
                                fileName += "." + contentType.Substring(contentType.LastIndexOf("/") + 1, contentType.LastIndexOf("codecs") - 8);
                            }
                            else
                            {
                                fileName += "." + contentType.Substring(contentType.LastIndexOf("/") + 1);
                            }


                            if (data.Data.Type.Equals("ptt"))
                            {
                                pastaDestino = "audios/";
                            }
                            else if (data.Data.Type.Equals("video"))
                            {
                                pastaDestino = "videos/";

                            }
                            else if (data.Data.Type.Equals("sticker"))
                            {
                                pastaDestino = "stickers/";

                            }
                            else if (data.Data.Type.Equals("image"))
                            {
                                pastaDestino = "images/";
                            }
                            else if (data.Data.Type.Equals("document"))
                            {
                                pastaDestino = "documents/";
                            }

                            wc.DownloadFile(url, "C:/zap_resources/" + pastaDestino + fileName);
                            Console.WriteLine($"Documento salvo com sucesso em {pastaDestino}{fileName}");

                            localMedia = "http:file://C:/zap_resources/" + pastaDestino + fileName;

                            var cmd = new NpgsqlCommand(InsertQuery);
                            cmd.Parameters.AddWithValue("event", data.EventType);
                            cmd.Parameters.AddWithValue("instanceid", data.InstanceId);
                            cmd.Parameters.AddWithValue("dataid", data.Data.Id);
                            cmd.Parameters.AddWithValue("datafrom", data.Data.From);
                            cmd.Parameters.AddWithValue("datato", data.Data.To);
                            cmd.Parameters.AddWithValue("dataack", data.Data.Ack);
                            cmd.Parameters.AddWithValue("datamedia", localMedia);
                            cmd.Parameters.AddWithValue("datatype", data.Data.Type);
                            cmd.Parameters.AddWithValue("databody", msgEncodada);
                            cmd.Parameters.AddWithValue("datafromme", data.Data.FromMe);
                            cmd.Parameters.AddWithValue("dataisforwarded", data.Data.IsForwarded);
                            cmd.Parameters.AddWithValue("datatime", Modulo.GetUnixToDate(data.Data.Time));
                            cmd.Parameters.AddWithValue("numerocli", numeroClienteSeparado);
                            cmd.Parameters.AddWithValue("reaction", "");
                            cmd.Parameters.AddWithValue("atendente", "");



                            int resultado = Modulo.SQL_executeNonQuery(cmd);
                            if (resultado == 1)
                            {
                                Console.WriteLine("Dado adicionado com sucesso as " + DateTime.Now.ToString());
                            }

                        });
                        downloadThread.Start();


                    }
                    else
                    {
                        var cmd = new NpgsqlCommand(InsertQuery);
                        cmd.Parameters.AddWithValue("event", data.EventType);
                        cmd.Parameters.AddWithValue("instanceid", data.InstanceId);
                        cmd.Parameters.AddWithValue("dataid", data.Data.Id);
                        cmd.Parameters.AddWithValue("datafrom", data.Data.From);
                        cmd.Parameters.AddWithValue("datato", data.Data.To);
                        cmd.Parameters.AddWithValue("dataack", data.Data.Ack);
                        cmd.Parameters.AddWithValue("datamedia", localMedia);
                        cmd.Parameters.AddWithValue("datatype", data.Data.Type);
                        cmd.Parameters.AddWithValue("databody", msgEncodada);
                        cmd.Parameters.AddWithValue("datafromme", data.Data.FromMe);
                        cmd.Parameters.AddWithValue("dataisforwarded", data.Data.IsForwarded);
                        cmd.Parameters.AddWithValue("datatime", Modulo.GetUnixToDate(data.Data.Time));
                        cmd.Parameters.AddWithValue("numerocli", numeroClienteSeparado);
                        cmd.Parameters.AddWithValue("reaction", "");
                        cmd.Parameters.AddWithValue("atendente", "");



                        int resultado = Modulo.SQL_executeNonQuery(cmd);
                        if (resultado == 1)
                        {
                            Console.WriteLine("Dado adicionado com sucesso as " + DateTime.Now.ToString());
                        }
                    }


                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("----------------- CATCH EXCEPTION -------------------");
                Console.WriteLine("Erro ao manipular com o banco de dados, caiu no catch. Erro: " + ex.Message);
            }


            return base.Ok(data);
        }


    }

}