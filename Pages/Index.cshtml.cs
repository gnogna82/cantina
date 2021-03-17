using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventHubs.Consumer;
using Google.DataTable.Net.Wrapper;
using System.Text;
using System.Threading;

namespace cantina.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public string RestartLabel {get; set;}

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }


        public async Task<ActionResult> OnGetChartData()
        {
            var connectionString = "Endpoint=sb://germanywestcentraldedns001.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=zotnWiuuXxJcWKjFKwktru/Sl10TXN54uj9zmNqdF5Y=";
            var eventHubName = "iothub-ehub-belvederei-7296405-6cf0df2855";
            var consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

            var consumer = new EventHubConsumerClient(
                consumerGroup,
                connectionString,
                eventHubName);


            var dt=new DataTable();
            dt.AddColumn(new Column(ColumnType.Datetime, "Time"));
            dt.AddColumn(new Column(ColumnType.Number, "Pompa"));
            dt.AddColumn(new Column(ColumnType.Number, "Sifone"));

            try
            {
                using CancellationTokenSource cancellationSource = new CancellationTokenSource();
                cancellationSource.CancelAfter(TimeSpan.FromSeconds(10));

                int eventsRead = 0;
                int maximumEvents = 300;
                string body="";
                DateTime _time;

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(cancellationSource.Token))
                {
                    string readFromPartition = partitionEvent.Partition.PartitionId;
                    body=partitionEvent.Data.EventBody.ToString();
                    _time=partitionEvent.Data.EnqueuedTime.DateTime;
                    
                    if (body=="Restarted")
                    {
                        RestartLabel+= _time.ToString();
                    }
                    else
                    {
                        Row r = dt.NewRow();
                        r.AddCellRange(new Cell[]
                            {
                                new Cell(_time),
                                new Cell(float.Parse(body.Substring(0,4))),
                                new Cell(float.Parse(body.Substring(4,4)))
                            });
                        dt.AddRow(r);
                    }
                    eventsRead++;

                    if (eventsRead >= maximumEvents)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                await consumer.CloseAsync();
            }

            return Content(dt.GetJson());
        }
    }
}
