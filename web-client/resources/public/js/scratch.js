/** @jsx React.DOM */

var Search = React.createClass({
    getInitialState: function() {
	return {searchText: "" };
    },
    handleSubmit: function() {
	//console.log(this.refs.searchInput.getDOMNode().value);
	var val = this.refs.searchInput.getDOMNode().value;
	this.props.onUserInput(val);
	return false;
    },
    handleChange: function() {
	var val = this.refs.searchInput.getDOMNode().value;
	console.log(val);
        this.setState({searchText: val});
    },
    render: function() {
	if (this.state.searchText == "dingo")
	    return (
	    <form onSubmit={this.handleSubmit}>
		<h3>Search</h3>
		<input type="text" 
	               value={this.state.searchText} 
	               ref="searchInput" 
	               onChange={this.handleChange}/>
	    </form>
	    )
	else
	{
	return (
	    <form onSubmit={this.handleSubmit}>
		<h3>Search</h3>
		<input type="text" 
	               value={this.state.searchText} 
	               ref="searchInput" 
	               onChange={this.handleChange}/>
		<button>Search</button>
	    </form>
	);
	}
    }
});

var SearchItemRow = React.createClass({
    render: function() {
	return (
	    <div className="item">
		<img className="item-box-left" src={this.props.thumbnail} />
		<div className="item-box-right">{this.props.text}</div>
	    </div>	
	);
    }
});

var SearchTable = React.createClass({
    getInitialState: function() {
	return {searchResults: []};
    },
    onUserInput: function(text) {
	console.log("Searching for " + text);
	$.get("http://localhost:3000/search-one?name=" + text, function(data) {
	    console.dir(data);
	    this.setState({searchResults: data});
	}.bind(this));

	return false;
    },
    render: function() { 
	var rows = [];
	this.state.searchResults.forEach(function(res) {
	    rows.push(<SearchItemRow thumbnail={res.thumbnail} 
		                     text={res.text}/>);
	});
	return (
	    <div className="row">
		<Search onUserInput={this.onUserInput}/>
		<h3>Search Results</h3>
		<div className="col-xs-12 col-sm-8 col-md-8 column-1">{rows}</div>
	    </div>
	);
    }
});

var MonitorItem = React.createClass({
    render: function() {
	return (
	    <div className="monitor">
		<div className="search-term">Monitoring {this.props.searchTerm}</div>
		<div className="number-of-new-items">{this.props.numberNewItems}</div> new items.
	    </div>	
	);
    }
});

var MonitorTable = React.createClass({
    render: function() {
	var rows = _.map(this.props.monitors, function(item) {
	    console.log("item " + item);
	    return <MonitorItem searchTerm={item} 
		                   numberNewItems={item.numberNewItems} />
	});
	return (
	    <div className="row">
		<h3>Monitors</h3>
		<div className="col-xs-12 col-sm-12 col-md-4 column-2">{rows}</div>
            </div>
	);
    }
});

var App = React.createClass({
    getInitialState: function() {
	$.get("/" + this.props.data.userId + "/monitors", function(data) {
	    console.dir(data);
 	    this.setState({monitors: data.monitors});
	}.bind(this));

	return {monitors: []};
    },
    render: function() {
	return (
	    <div className="row">
		<div className="col-xs-12 col-sm-12 col-md-4 column-2">
		   <MonitorTable monitors={this.state.monitors} />
		</div>
            </div>
	);
    }
})

var data = {
    monitorItems: [{"searchTerm": "fisk", "numberNewItems": 4},
		   {"searchTerm": "dingo", "numberNewItems": 2}],
    searchItems: [],
    userId: 123,
};

var root = React.renderComponent(<App data={data} />, 
				 document.getElementById('app'));


//var root = React.renderComponent(<MonitorTable monitors={data.monitorItems} />, 
//				 document.getElementById('monitor'));

//React.renderComponent(<SearchTable searchResults={data.searchItems} />, 
//		      document.getElementById('app'));


var FeedReader = function(feed) {

    var item = {};

    var readBackwardsHelper = function(link, steps, itemCallback, acc) {
	if(steps <= 0 || link === undefined) {
	    console.dir(acc);
	    _.each(acc.reverse(), function(accFunc) { accFunc(); });
	    return;
	}
	$.get(link.href, function(data) {
	    if(data._links.prev != undefined) {
		acc.push(function() { itemCallback(data, link.href); });
		readBackwardsHelper(data._links.prev, --steps, itemCallback, acc);
		};
	    });
    };

    // delivers items to itemCallback in order oldest -> newest
    var readBackwards = function(link, steps, itemCallback) {
	readBackwardsHelper(link, steps, itemCallback, []);
    };

    var readForwards = function(link, steps, itemCallback) {
	if(steps <= 0 || link === undefined) {
	    return;
	}
	$.get(link.href, function(data) {
	    itemCallback(data, link);
	    if(data._links.next != undefined) {
		readForwards(data._links.next, --steps, itemCallback);
	    }
	});
    };

    item.readBackwards = readBackwards;
    item.readForwards = readForwards;

    item.readNewest = function(count, itemCallback) {
	$.get("http://localhost:3000/feed/stokke", function(data) {
	    if(data._links.newest != undefined) {
		readBackwards(data._links.newest, count, itemCallback);
	    }
	});
    }

    return item;
};

var reader = FeedReader("http://localhost:3000/feed/stokke");

var SearchTest = React.createClass({

    traverseForward: function(link, steps, itemCallback){
	if(steps <= 0 || link === undefined) {
	    return;
	}
	console.log("getting " + link.href);
	$.get(link.href, function(data) {
	    if(data._links.next != undefined) {
		itemCallback(data);
		this.traverseForward(data._links.next, --steps, itemCallback);
	    }
	}.bind(this));
    },

    checkStream: function(){
	$.get("http://localhost:3000/feed/stokke", function(data) {
	    console.dir(data);
	    if(data._links.next != undefined) {
		this.traverseForward(data._links.next, 20, function(item) {
		    //console.dir(item);
		});
	    }
	}.bind(this));
    },
    onClick: function() {
	console.log("Clicked");

	reader.readNewest(4, function(data, link) {
	    console.log(data._embedded.text);
	    console.log(link);
	});
	return;
	$.get("http://localhost:3000/feed/stokke", function(data) {
	    console.dir(data._embedded.text);
	});

	
    },

    createStream: function(){
	console.dir("her");
	$.post("http://localhost:3000/feed/stokke", function(data) {
	    console.dir("Received ");
	    console.dir(data);
	});
    },

    render: function() {
	return (
	    <div>
		<button onClick={this.onClick}>Query</button>
        	<button onClick={this.createStream}>Create Stream</button>
        	<button onClick={this.checkStream}>ReadStream</button>
            </div>
	);
    }
});
React.renderComponent(<SearchTest />, document.getElementById('example'));


var TabLocation = React.createClass({

    componentDidMount: function() {
	crossroads.addRoute(this.props.section);
    },
    buttonClick: function(event) {
	event.preventDefault();
	crossroads.parse('/' + this.props.section);
	hasher.setHash(this.props.section);
	this.props.f();
    },
    render: function() {
	return(
	    <button onClick={this.buttonClick}>{this.props.text}</button>
	);
    }
});
