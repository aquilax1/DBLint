$(document).ready(function() { 

				$.tablesorter.addParser({ 
				// set a unique id 
				id: 'severities', 
				is: function(s) { 
					// return false so this parser is not auto detected 
					return false; 
				}, 
				format: function(s) { 
					// format your data for normalization 
					return s.toLowerCase().replace(/critical/,4).replace(/high/,3).replace(/medium/,2).replace(/low/,1); 
				}, 
				// set type, either numeric or text 
				type: 'numeric' 
			}); 

				$("#tabs").tabs();
				$("#all-tables").tablesorter();
				$("#rule_table").tablesorter({ 
					headers: { 
						1: {
							sorter:'severities' 
						}
					} 
				});
				$("#issues_summary").tablesorter({ 
					headers: { 
						1: { 
							sorter:'severities' 
						}
					} 
				});
	
				$("#issueFrame").load( function() 
				{
					resize_frame($('#issueFrame'));
				});

				$("#incrementFrame").load( function() 
				{
					resize_frame($('#incrementFrame'));
				});
		});

		function resize_frame(f)
		{
			f.height(0);
			var h = f.contents().height();
			if (h == null) //Chrome
			{
				f.height($(window).height()-200);
			}
			else
			{
				f.height(h+300);
			}
		}
		
		function show_issue_list(list_id)
		{
			var f = $('#issueFrame');
			f.attr('src', 'issues/'+list_id+'.html');
		}

		function show_diff(diff_id)
		{
			var f = $('#incrementFrame');
			f.attr('src', 'increments/'+diff_id+'.html');
		}

		function show_issue_tab(list_id)
		{
			$("#tabs").tabs('select', 'tabs-issues');
			show_issue_list(list_id);
		}

		function toggleMoreLess(link)
		{
			if (link.html() == 'more')
				link.html('less');
			else
				link.html('more');
		}